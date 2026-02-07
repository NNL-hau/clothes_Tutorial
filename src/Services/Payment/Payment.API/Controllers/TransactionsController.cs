using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            string vnp_HashSecret = _configuration["VnPay:HashSecret"]!;

            bool isValidSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);

            if (!isValidSignature)
            {
                _logger.LogWarning("VNPay Callback: Invalid Signature for TxnRef: {TxnRef}", txnRef);
                return BadRequest("Invalid signature");
            }

            // Redirect back to client with status
            var returnUrl = _configuration["VnPay:ReturnUrl"] ?? "http://localhost:5000/checkout";
            return Redirect($"{returnUrl}?vnp_TxnRef={txnRef}&vnp_ResponseCode={responseCode}");
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
            string vnp_TransactionStatus = vnpay.GetResponseData("vnp_TransactionStatus");
            string vnp_SecureHash = Request.Query["vnp_SecureHash"]!;
            string vnp_HashSecret = _configuration["VnPay:HashSecret"]!;

            bool isValidSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);

            if (!isValidSignature)
            {
                return Ok(new { RspCode = "97", Message = "Invalid signature" });
            }

            if (!Guid.TryParse(txnRef, out var transactionId))
            {
                return Ok(new { RspCode = "01", Message = "Order not found" });
            }

            var transaction = await _context.Transactions.FindAsync(transactionId);
            if (transaction == null)
            {
                return Ok(new { RspCode = "01", Message = "Order not found" });
            }

            // Check if transaction already confirmed
            if (transaction.Status != "Pending")
            {
                return Ok(new { RspCode = "02", Message = "Order already confirmed" });
            }

            // Validate Amount (vnp_Amount is multiplied by 100)
            long vnp_Amount = Convert.ToInt64(vnpay.GetResponseData("vnp_Amount")) / 100;
            if (transaction.Amount != vnp_Amount)
            {
                return Ok(new { RspCode = "04", Message = "Invalid amount" });
            }

            if (responseCode == "00" && vnp_TransactionStatus == "00")
            {
                transaction.Status = "Success";
                _logger.LogInformation("IPN: Transaction {TxnRef} successful. Updating Order {OrderId} to Pending.", txnRef, transaction.OrderId);
                
                try 
                {
                    using var client = new HttpClient();
                    var orderingUrl = _configuration["OrderingApiUrl"] ?? "http://ordering-api/api/orders";
                    var updateDto = new { Status = "Pending" };
                    var response = await client.PatchAsJsonAsync($"{orderingUrl}/{transaction.OrderId}/status", updateDto);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError("IPN: Failed to update Order {OrderId} status: {Status}", transaction.OrderId, response.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "IPN: Error calling Ordering API for Order {OrderId}", transaction.OrderId);
                }
            }
            else
            {
                transaction.Status = "Failed";
                _logger.LogWarning("IPN: Transaction {TxnRef} failed with code {Code}", txnRef, responseCode);
            }

            await _context.SaveChangesAsync();
            return Ok(new { RspCode = "00", Message = "Confirm Success" });
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
            var vnp_TmnCode = _configuration["VnPay:TmnCode"];
            var vnp_HashSecret = _configuration["VnPay:HashSecret"];
            var vnp_Url = _configuration["VnPay:BaseUrl"];
            
            var vnp_CallbackUrl = _configuration["VnPay:CallbackUrl"];

            vnpay.AddRequestData("vnp_Version", _configuration["VnPay:Version"] ?? "2.1.0");
            vnpay.AddRequestData("vnp_Command", _configuration["VnPay:Command"] ?? "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode!);
            vnpay.AddRequestData("vnp_Amount", ((long)(transaction.Amount * 100)).ToString());
            vnpay.AddRequestData("vnp_CreateDate", transaction.CreatedAt.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", _configuration["VnPay:CurrCode"] ?? "VND");
            
            string ipAddr = GetClientIpAddress();
            vnpay.AddRequestData("vnp_IpAddr", ipAddr);
            
            vnpay.AddRequestData("vnp_Locale", _configuration["VnPay:Locale"] ?? "vn");
            
            string orderInfo = $"Payment_Order_{transaction.OrderId.ToString().Substring(0, 8)}";
            vnpay.AddRequestData("vnp_OrderInfo", orderInfo);
            vnpay.AddRequestData("vnp_OrderType", "other");
            
            vnpay.AddRequestData("vnp_ExpireDate", transaction.CreatedAt.AddMinutes(15).ToString("yyyyMMddHHmmss"));

            // Commented out billing info for baseline testing - simple request is more likely to succeed
            /*
            if (!string.IsNullOrEmpty(dto.FullName))
            {
                vnpay.AddRequestData("vnp_Bill_Mobile", dto.PhoneNumber ?? "");
                vnpay.AddRequestData("vnp_Bill_Email", dto.Email ?? "");
                var names = dto.FullName.Trim().Split(' ');
                if (names.Length > 1)
                {
                    vnpay.AddRequestData("vnp_Bill_FirstName", names.Last());
                    vnpay.AddRequestData("vnp_Bill_LastName", string.Join(" ", names.Take(names.Length - 1)));
                }
                else
                {
                    vnpay.AddRequestData("vnp_Bill_FirstName", dto.FullName);
                }
            }
            */

            vnpay.AddRequestData("vnp_ReturnUrl", vnp_CallbackUrl!);
            vnpay.AddRequestData("vnp_TxnRef", transaction.Id.ToString());

            _logger.LogInformation("VNPay Request Parameters: TmnCode={TmnCode}, Amount={Amount}, TxnRef={TxnRef}, ReturnUrl={ReturnUrl}, IpAddr={IpAddr}", 
                vnp_TmnCode, transaction.Amount * 100, transaction.Id, vnp_CallbackUrl, ipAddr);

            return vnpay.CreateRequestUrl(vnp_Url!, vnp_HashSecret!);
        }

        private string GetClientIpAddress()
        {
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
