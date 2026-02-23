using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Payment.API.Data;
using Payment.API.Models;
using Payment.API.DTOs;

namespace Payment.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionsController : ControllerBase
    {
        private readonly PaymentDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TransactionsController> _logger;

        public TransactionsController(PaymentDbContext context, IConfiguration configuration, ILogger<TransactionsController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TransactionDto>>> GetTransactions()
        {
            _logger.LogInformation("Fetching all transactions");
            return await _context.Transactions
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TransactionDto(t.Id, t.OrderId, t.UserName, t.Amount, t.PaymentMethod, t.Status, t.CreatedAt, null))
                .ToListAsync();
        }

        /// <summary>
        /// DEMO MODE: Simple status update endpoint for frontend (no signature verification)
        /// </summary>
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateTransactionStatus(Guid id, [FromBody] UpdateStatusRequest request)
        {
            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction == null)
            {
                return NotFound(new { message = "Transaction not found" });
            }

            transaction.Status = request.Status;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Transaction {Id} status updated to {Status} (DEMO MODE - no verification)", id, request.Status);

            // If successful, update order status
            if (request.Status == "Success")
            {
                try
                {
                    using var client = new HttpClient();
                    var orderingUrl = _configuration["OrderingApiUrl"] ?? "http://ordering-api/api/orders";
                    var updateDto = new { Status = "Pending" };
                    var response = await client.PatchAsJsonAsync($"{orderingUrl}/{transaction.OrderId}/status", updateDto);

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError("Failed to update Order {OrderId} status", transaction.OrderId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calling Ordering API for Order {OrderId}", transaction.OrderId);
                }
            }

            return Ok(new { message = "Status updated successfully", status = transaction.Status });
        }


        [HttpGet("callback")]
        public async Task<IActionResult> ProcessCallback()
        {
            _logger.LogInformation("Processing VNPay Callback (ReturnURL): {Query}", Request.QueryString);
            
            var vnpayData = Request.Query;
            var vnpay = new Utils.VnPayLibrary();

            foreach (var key in vnpayData.Keys)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    vnpay.AddResponseData(key, vnpayData[key]!);
                }
            }

            string txnRef = vnpay.GetResponseData("vnp_TxnRef");
            string responseCode = vnpay.GetResponseData("vnp_ResponseCode");
            string vnp_SecureHash = Request.Query["vnp_SecureHash"]!;
            string vnp_HashSecret = _configuration["Payment:VNPay:HashSecret"]!;
            bool isValidSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);

            if (isValidSignature)
            {
                _logger.LogInformation("[PAYMENT_VERIFY] VNPay Callback: Signature Valid. Updating status for TxnRef: {TxnRef}", txnRef);
                await UpdateTransactionStatus(txnRef, responseCode, vnpay.GetResponseData("vnp_TransactionStatus"));
            }
            else
            {
                _logger.LogError("[PAYMENT_VERIFY] VNPay Callback: INVALID SIGNATURE for TxnRef: {TxnRef}", txnRef);
                responseCode = "99"; // Signal signature error to client
            }

            // Redirect back to client with status aligned with frontend (CheckoutResult.razor)
            var webAppUrl = _configuration["WebAppUrl"] ?? "http://localhost:5078";
            
            bool isSuccess = responseCode == "00" && isValidSignature;
            string msg = isSuccess ? "Thanh toán thành công" : "Thanh toán thất bại hoặc lỗi chữ ký";
            
            // Fetch transaction to get OrderId
            string orderId = "";
            if (Guid.TryParse(txnRef, out var tId))
            {
                var transaction = await _context.Transactions.FindAsync(tId);
                orderId = transaction?.OrderId.ToString() ?? "";
            }

            return Redirect($"{webAppUrl}/checkout/result?success={isSuccess.ToString().ToLower()}&orderId={orderId}&message={WebUtility.UrlEncode(msg)}");
        }

        [HttpGet("vnpay-ipn")]
        public async Task<IActionResult> ProcessIpn()
        {
            _logger.LogInformation("Processing VNPay IPN (Server-to-Server): {Query}", Request.QueryString);
            
            var vnpayData = Request.Query;
            var vnpay = new Utils.VnPayLibrary();

            foreach (var key in vnpayData.Keys)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    vnpay.AddResponseData(key, vnpayData[key]!);
                }
            }

            string txnRef = vnpay.GetResponseData("vnp_TxnRef");
            string responseCode = vnpay.GetResponseData("vnp_ResponseCode");
            string vnp_SecureHash = Request.Query["vnp_SecureHash"]!;
            string vnp_HashSecret = _configuration["Payment:VNPay:HashSecret"]!;

            bool isValidSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);

            if (!isValidSignature)
            {
                return Ok(new { RspCode = "97", Message = "Invalid signature" });
            }

            var result = await UpdateTransactionStatus(txnRef, responseCode, vnpay.GetResponseData("vnp_TransactionStatus"), vnpay.GetResponseData("vnp_Amount"));
            
            if (result == "Success") return Ok(new { RspCode = "00", Message = "Confirm Success" });
            if (result == "AlreadyConfirmed") return Ok(new { RspCode = "02", Message = "Order already confirmed" });
            if (result == "InvalidAmount") return Ok(new { RspCode = "04", Message = "Invalid amount" });
            
            return Ok(new { RspCode = "01", Message = "Order not found" });
        }

        private async Task<string> UpdateTransactionStatus(string txnRef, string responseCode, string transactionStatus, string? vnpAmountStr = null)
        {
            if (!Guid.TryParse(txnRef, out var transactionId))
            {
                return "NotFound";
            }

            var transaction = await _context.Transactions.FindAsync(transactionId);
            if (transaction == null)
            {
                return "NotFound";
            }

            // Check if transaction already confirmed (Success or Failed)
            if (transaction.Status != "Pending")
            {
                return "AlreadyConfirmed";
            }

            // Validate Amount if provided (vnp_Amount is multiplied by 100)
            if (!string.IsNullOrEmpty(vnpAmountStr))
            {
                long vnp_Amount = Convert.ToInt64(vnpAmountStr) / 100;
                if (transaction.Amount != vnp_Amount)
                {
                    return "InvalidAmount";
                }
            }

            // VNPay 2.1.0: responseCode 00 means successful communication, transactionStatus 00 means successful payment
            // However, transactionStatus might be missing in some ReturnUrl data, so we check responseCode primarily
            if (responseCode == "00" && (string.IsNullOrEmpty(transactionStatus) || transactionStatus == "00"))
            {
                transaction.Status = "Success";
                _logger.LogInformation("[PAYMENT_VERIFY] Transaction {TxnRef} successful. Updating Order {OrderId} to Pending.", txnRef, transaction.OrderId);
                
                try 
                {
                    using var client = new HttpClient();
                    var orderingUrl = _configuration["OrderingApiUrl"] ?? "http://ordering-api/api/orders";
                    var updateDto = new { Status = "Pending" };
                    var response = await client.PatchAsJsonAsync($"{orderingUrl}/{transaction.OrderId}/status", updateDto);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        _logger.LogError("Failed to update Order {OrderId} status: {Status}, Error: {Error}", transaction.OrderId, response.StatusCode, error);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calling Ordering API for Order {OrderId}", transaction.OrderId);
                }
            }
            else
            {
                transaction.Status = "Failed";
                _logger.LogWarning("[PAYMENT_VERIFY] Transaction {TxnRef} failed with code {Code}", txnRef, responseCode);
            }

            await _context.SaveChangesAsync();
            return "Success";
        }

        /// <summary>
        /// Tạo giao dịch thanh toán mới (dùng khi khách hàng thanh toán đơn hàng từ giỏ hàng).
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<TransactionDto>> CreateTransaction([FromBody] CreateTransactionDto dto)
        {
            _logger.LogInformation("[BUILD_VER_FINAL_V2] Processing {Method} for {User}", dto.PaymentMethod, dto.UserName);
            
            bool isVnPay = string.Equals(dto.PaymentMethod, "VNPay", StringComparison.OrdinalIgnoreCase);
            bool isCod = string.Equals(dto.PaymentMethod, "COD", StringComparison.OrdinalIgnoreCase);

            var transaction = new Transaction
            {
                OrderId = dto.OrderId,
                UserName = dto.UserName,
                Amount = dto.Amount,
                PaymentMethod = dto.PaymentMethod,
                Status = isCod ? "Success" : "Pending"
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            if (isCod)
            {
                 _logger.LogInformation("COD Transaction {Id} created. Order {OrderId} is ready for processing.", transaction.Id, transaction.OrderId);
            }

            string? paymentUrl = null;
            if (isVnPay)
            {
                _logger.LogInformation("Creating VNPay transaction for Order: {OrderId}, Amount: {Amount}", dto.OrderId, dto.Amount);
                try 
                {
                    paymentUrl = GenerateVnPayUrl(transaction, dto);
                    _logger.LogInformation("VNPay URL Generated: {Url}", paymentUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating VNPay URL");
                }
            }

            var result = new TransactionDto(
                transaction.Id,
                transaction.OrderId,
                transaction.UserName,
                transaction.Amount,
                transaction.PaymentMethod,
                transaction.Status,
                transaction.CreatedAt,
                paymentUrl
            );

            return CreatedAtAction(nameof(GetTransactions), new { id = transaction.Id }, result);
        }

        private string GenerateVnPayUrl(Transaction transaction, CreateTransactionDto dto)
        {
            var vnpay = new Utils.VnPayLibrary();
            var vnp_TmnCode = _configuration["Payment:VNPay:TmnCode"] ?? "";
            var vnp_HashSecret = _configuration["Payment:VNPay:HashSecret"] ?? "";
            var vnp_Url = _configuration["Payment:VNPay:PaymentUrl"] ?? "";
            var vnp_ReturnUrl = _configuration["Payment:VNPay:ReturnUrl"] ?? "";

            if (string.IsNullOrEmpty(vnp_TmnCode) || string.IsNullOrEmpty(vnp_HashSecret))
            {
                throw new Exception("VNPay Configuration is missing TmnCode or HashSecret");
            }

            // ===== VNPay yêu cầu giờ Việt Nam (UTC+7) =====
            var vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            var vnTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
            string vnp_CreateDate = vnTime.ToString("yyyyMMddHHmmss");
            string vnp_ExpireDate = vnTime.AddMinutes(15).ToString("yyyyMMddHHmmss");

            vnpay.AddRequestData("vnp_Version", _configuration["Payment:VNPay:Version"] ?? "2.1.0");
            vnpay.AddRequestData("vnp_Command", _configuration["Payment:VNPay:Command"] ?? "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode.Trim());
            vnpay.AddRequestData("vnp_Amount", ((long)(transaction.Amount * 100)).ToString());
            vnpay.AddRequestData("vnp_CurrCode", _configuration["Payment:VNPay:CurrCode"] ?? "VND");
            vnpay.AddRequestData("vnp_BankCode", ""); // Default to empty to allow user to choose on VNPay portal
            vnpay.AddRequestData("vnp_CreateDate", vnp_CreateDate);
            vnpay.AddRequestData("vnp_IpAddr", GetClientIpAddress());
            vnpay.AddRequestData("vnp_Locale", _configuration["Payment:VNPay:Locale"] ?? "vn");

            // vnp_OrderInfo không nên có dấu và không nên quá phức tạp
            vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang");
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", vnp_ReturnUrl);
            vnpay.AddRequestData("vnp_TxnRef", transaction.Id.ToString());
            vnpay.AddRequestData("vnp_ExpireDate", vnp_ExpireDate);

            string finalUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret.Trim());
            _logger.LogInformation("VNPay Request URL: {Url}", finalUrl);
            return finalUrl;
        }

        private string GetClientIpAddress()
        {
            // Lấy IP client (đã qua middleware ForwardedHeaders)
            var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (ips.Length > 0)
                {
                    var ip = ips[0].Trim();
                    return ip;
                }
            }

            var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            if (!string.IsNullOrEmpty(remoteIp))
            {
                if (remoteIp == "::1" || remoteIp.Contains("::"))
                {
                    return "127.0.0.1";
                }
                return remoteIp;
            }

            return "127.0.0.1";
        }
    }
}
