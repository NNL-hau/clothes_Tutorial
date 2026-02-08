using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Payment.API.Data;
using Payment.API.Utils;

namespace Payment.API.Controllers
{
    [ApiController]
    [Route("payment")]
    public class PaymentController : ControllerBase
    {
        private readonly PaymentDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(PaymentDbContext context, IConfiguration configuration, ILogger<PaymentController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// VNPay Return URL - Xử lý khi user quay về từ VNPay
        /// </summary>
        [HttpGet("vnpay-return")]
        public async Task<IActionResult> VNPayReturn()
        {
            try
            {
                _logger.LogInformation("VNPay Return URL callback received: {Query}", Request.QueryString);
                
                var vnpayData = Request.Query;
                var vnpay = new VnPayLibrary();

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
                    _logger.LogWarning("VNPay Return: Invalid Signature for TxnRef: {TxnRef}", txnRef);
                    return Redirect($"http://localhost:5078/checkout/result?success=false&message=Invalid signature");
                }

                // Update transaction status
                if (Guid.TryParse(txnRef, out var transactionId))
                {
                    var transaction = await _context.Transactions.FindAsync(transactionId);
                    if (transaction != null && transaction.Status == "Pending")
                    {
                        if (responseCode == "00")
                        {
                            transaction.Status = "Success";
                            _logger.LogInformation("Transaction {TxnRef} successful. Updating Order {OrderId}.", txnRef, transaction.OrderId);
                            
                            // Update order status
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
                        else
                        {
                            transaction.Status = "Failed";
                            _logger.LogWarning("Transaction {TxnRef} failed with code {Code}", txnRef, responseCode);
                        }

                        await _context.SaveChangesAsync();
                    }
                }

                // Redirect to result page
                if (responseCode == "00")
                {
                    return Redirect($"http://localhost:5078/checkout/result?success=true&orderId={txnRef}");
                }
                else
                {
                    return Redirect($"http://localhost:5078/checkout/result?success=false&message=Payment failed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing VNPay callback");
                return Redirect($"http://localhost:5078/checkout/result?success=false&message=System error");
            }
        }

        /// <summary>
        /// MoMo Return URL - Xử lý khi user quay về từ MoMo
        /// </summary>
        [HttpGet("momo-return")]
        public async Task<IActionResult> MoMoReturn()
        {
            try
            {
                _logger.LogInformation("MoMo Return URL callback received: {Query}", Request.QueryString);
                
                var queryParams = Request.Query.ToDictionary(
                    x => x.Key,
                    x => x.Value.ToString()
                );

                foreach (var param in queryParams)
                {
                    _logger.LogInformation($"MoMo Param: {param.Key} = {param.Value}");
                }

                var orderCode = queryParams.GetValueOrDefault("orderId", "");
                var resultCode = queryParams.GetValueOrDefault("resultCode", "");
                
                if (string.IsNullOrEmpty(orderCode))
                {
                    return Redirect($"http://localhost:5078/checkout/result?success=false&message=Missing order code");
                }

                // Update transaction status
                if (Guid.TryParse(orderCode, out var transactionId))
                {
                    var transaction = await _context.Transactions.FindAsync(transactionId);
                    if (transaction != null && transaction.Status == "Pending")
                    {
                        if (resultCode == "0")
                        {
                            transaction.Status = "Success";
                            _logger.LogInformation("MoMo Transaction {OrderId} successful", orderCode);
                        }
                        else
                        {
                            transaction.Status = "Failed";
                            _logger.LogWarning("MoMo Transaction {OrderId} failed with code {Code}", orderCode, resultCode);
                        }

                        await _context.SaveChangesAsync();
                    }
                }

                if (resultCode == "0")
                {
                    return Redirect($"http://localhost:5078/checkout/result?success=true&orderId={orderCode}");
                }
                else
                {
                    return Redirect($"http://localhost:5078/checkout/result?success=false&message=Payment failed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MoMo callback");
                return Redirect($"http://localhost:5078/checkout/result?success=false&message=System error");
            }
        }

        /// <summary>
        /// MoMo IPN (Instant Payment Notification)
        /// </summary>
        [HttpPost("momo-notify")]
        public async Task<IActionResult> MoMoNotify([FromBody] Dictionary<string, string> notification)
        {
            try
            {
                _logger.LogInformation("MoMo IPN received");
                
                var orderCode = notification.GetValueOrDefault("orderId", "");
                var resultCode = notification.GetValueOrDefault("resultCode", "");
                
                if (Guid.TryParse(orderCode, out var transactionId))
                {
                    var transaction = await _context.Transactions.FindAsync(transactionId);
                    if (transaction != null && transaction.Status == "Pending")
                    {
                        transaction.Status = resultCode == "0" ? "Success" : "Failed";
                        await _context.SaveChangesAsync();
                    }
                }

                return Ok(new { success = true, message = "Processed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MoMo IPN");
                return Ok(new { success = false, message = "System error" });
            }
        }
    }
}
