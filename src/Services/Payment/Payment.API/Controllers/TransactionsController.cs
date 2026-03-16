using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
                .Select(t => new TransactionDto(t.Id, t.OrderId, t.UserName, t.Amount, t.PaymentMethod, t.Status, t.CreatedAt, null, null))
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
            bool isMoMo = string.Equals(dto.PaymentMethod, "MoMo", StringComparison.OrdinalIgnoreCase);
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
            string? qrCodeUrl = null;
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
            else if (isMoMo)
            {
                _logger.LogInformation("Creating MoMo transaction for Order: {OrderId}, Amount: {Amount}", dto.OrderId, dto.Amount);
                try
                {
                    var momoResult = await GenerateMoMoUrl(transaction, dto);
                    paymentUrl = momoResult.PayUrl;
                    qrCodeUrl  = momoResult.QrCodeUrl;
                    _logger.LogInformation("MoMo PayUrl: {PayUrl}, QrCodeUrl: {QrCodeUrl}", paymentUrl, qrCodeUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating MoMo URL");
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
                paymentUrl,
                qrCodeUrl
            );

            return CreatedAtAction(nameof(GetTransactions), new { id = transaction.Id }, result);
        }

        /// <summary>
        /// MoMo sandbox callback endpoint - MoMo redirects user back here after payment
        /// </summary>
        [HttpGet("momo-return")]
        public async Task<IActionResult> MoMoReturn()
        {
            _logger.LogInformation("MoMo Return URL hit: {Query}", Request.QueryString);

            var query = Request.Query;
            string orderId      = query["orderId"].ToString();
            string resultCode   = query["resultCode"].ToString();
            string message      = query["message"].ToString();
            string signature    = query["signature"].ToString();
            string requestId    = query["requestId"].ToString();
            string amount       = query["amount"].ToString();
            string partnerCode  = query["partnerCode"].ToString();
            string orderInfo    = query["orderInfo"].ToString();
            string orderType    = query["orderType"].ToString();
            string transId      = query["transId"].ToString();
            string responseTime = query["responseTime"].ToString();
            string payType      = query["payType"].ToString();
            string extraData    = query["extraData"].ToString();

            // Verify signature
            var secretKey = _configuration["Payment:MoMo:SecretKey"] ?? "";
            var rawHash = $"accessKey={_configuration["Payment:MoMo:AccessKey"]}" +
                          $"&amount={amount}" +
                          $"&extraData={extraData}" +
                          $"&message={message}" +
                          $"&orderId={orderId}" +
                          $"&orderInfo={orderInfo}" +
                          $"&orderType={orderType}" +
                          $"&partnerCode={partnerCode}" +
                          $"&payType={payType}" +
                          $"&requestId={requestId}" +
                          $"&responseTime={responseTime}" +
                          $"&resultCode={resultCode}" +
                          $"&transId={transId}";

            var computedSignature = HmacSHA256(rawHash, secretKey);
            bool isValidSignature = computedSignature.Equals(signature, StringComparison.OrdinalIgnoreCase);

            // orderId từ MoMo chính là transaction.Id (Guid dạng string)
            bool isSuccess = resultCode == "0" && isValidSignature;

            string dbOrderId = "";
            if (Guid.TryParse(orderId, out var transactionId))
            {
                var transaction = await _context.Transactions.FindAsync(transactionId);
                if (transaction != null)
                {
                    if (transaction.Status == "Pending")
                    {
                        transaction.Status = isSuccess ? "Success" : "Failed";
                        await _context.SaveChangesAsync();
                        dbOrderId = transaction.OrderId.ToString();

                        if (isSuccess)
                        {
                            try
                            {
                                using var client = new HttpClient();
                                var orderingUrl = _configuration["OrderingApiUrl"] ?? "http://ordering-api/api/orders";
                                var updateDto = new { Status = "Pending" };
                                await client.PatchAsJsonAsync($"{orderingUrl}/{transaction.OrderId}/status", updateDto);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error updating Order {OrderId} after MoMo success", transaction.OrderId);
                            }
                        }
                    }
                    else
                    {
                        dbOrderId = transaction.OrderId.ToString();
                    }
                }
            }

            var webAppUrl = _configuration["WebAppUrl"] ?? "http://localhost:5078";
            string msg = isSuccess ? "Thanh toán MoMo thành công" : $"Thanh toán MoMo thất bại: {message}";
            return Redirect($"{webAppUrl}/checkout?momo_result={resultCode}&orderId={dbOrderId}&message={WebUtility.UrlEncode(msg)}");
        }

        /// <summary>
        /// MoMo IPN - server-to-server notification
        /// </summary>
        [HttpPost("momo-notify")]
        public async Task<IActionResult> MoMoNotify([FromBody] JsonElement body)
        {
            _logger.LogInformation("MoMo IPN received");
            try
            {
                string orderId    = body.GetProperty("orderId").GetString() ?? "";
                string resultCode = body.GetProperty("resultCode").GetInt32().ToString();

                if (Guid.TryParse(orderId, out var transactionId))
                {
                    var transaction = await _context.Transactions.FindAsync(transactionId);
                    if (transaction != null && transaction.Status == "Pending")
                    {
                        transaction.Status = resultCode == "0" ? "Success" : "Failed";
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MoMo IPN");
            }
            return Ok(new { message = "IPN received" });
        }

        private record MoMoUrlResult(string? PayUrl, string? QrCodeUrl);

        private async Task<MoMoUrlResult> GenerateMoMoUrl(Transaction transaction, CreateTransactionDto dto)
        {
            var partnerCode = _configuration["Payment:MoMo:PartnerCode"] ?? "MOMO";
            var accessKey   = _configuration["Payment:MoMo:AccessKey"] ?? "";
            var secretKey   = _configuration["Payment:MoMo:SecretKey"] ?? "";
            var paymentUrl  = _configuration["Payment:MoMo:PaymentUrl"] ?? "";
            var returnUrl   = _configuration["Payment:MoMo:ReturnUrl"] ?? "";
            var notifyUrl   = _configuration["Payment:MoMo:NotifyUrl"] ?? "";

            var requestId   = transaction.Id.ToString();
            var orderId     = transaction.Id.ToString();
            var amount      = ((long)transaction.Amount).ToString();
            var orderInfo   = "Thanh toan don hang";
            var extraData   = "";
            // payWithMethod trả về cả payUrl lẫn qrCodeUrl
            var requestType = "payWithMethod";

            var rawHash = $"accessKey={accessKey}" +
                          $"&amount={amount}" +
                          $"&extraData={extraData}" +
                          $"&ipnUrl={notifyUrl}" +
                          $"&orderId={orderId}" +
                          $"&orderInfo={orderInfo}" +
                          $"&partnerCode={partnerCode}" +
                          $"&redirectUrl={returnUrl}" +
                          $"&requestId={requestId}" +
                          $"&requestType={requestType}";

            var signature = HmacSHA256(rawHash, secretKey);

            var momoRequest = new
            {
                partnerCode,
                partnerName = "ClothesShop Demo",
                storeId     = partnerCode,
                requestId,
                amount,
                orderId,
                orderInfo,
                redirectUrl = returnUrl,
                ipnUrl      = notifyUrl,
                lang        = "vi",
                extraData,
                requestType,
                signature
            };

            using var httpClient = new HttpClient();
            var json     = JsonSerializer.Serialize(momoRequest);
            var content  = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(paymentUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("MoMo API response: {Body}", responseBody);

            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            if (root.TryGetProperty("resultCode", out var rcProp) && rcProp.GetInt32() == 0)
            {
                var payUrl     = root.TryGetProperty("payUrl",     out var p) ? p.GetString() : null;
                var qrCodeUrl  = root.TryGetProperty("qrCodeUrl",  out var q) ? q.GetString() : null;
                var deeplink   = root.TryGetProperty("deeplink",   out var d) ? d.GetString() : null;
                _logger.LogInformation("MoMo payUrl={PayUrl}, qrCodeUrl={QrCodeUrl}, deeplink={Deeplink}", payUrl, qrCodeUrl, deeplink);
                return new MoMoUrlResult(payUrl, qrCodeUrl);
            }

            var errMsg = root.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : "Unknown MoMo error";
            throw new Exception($"MoMo API error: {errMsg} | Body: {responseBody}");
        }

        private static string HmacSHA256(string data, string key)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToHexString(hash).ToLower();
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
