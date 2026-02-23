using Microsoft.AspNetCore.Mvc;
using Payment_Service.Services;
using Payment_Service.Models;
using Payment_Service.Enums;
using Payment_Service.Helpers;

namespace Payment_Service.Examples
{
    /// <summary>
    /// Controller mẫu minh họa cách sử dụng Payment Service
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ExamplePaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public ExamplePaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        /// <summary>
        /// Tạo thanh toán COD
        /// </summary>
        [HttpPost("cod")]
        public async Task<IActionResult> CreateCODPayment([FromBody] CreatePaymentRequest request)
        {
            var paymentRequest = new PaymentRequestModel
            {
                OrderId = request.OrderId,
                Amount = request.Amount,
                PaymentMethod = PaymentMethod.COD,
                OrderDescription = request.Description,
                CustomerName = request.CustomerName,
                CustomerEmail = request.CustomerEmail,
                CustomerPhone = request.CustomerPhone,
                ShippingAddress = request.ShippingAddress
            };

            var response = await _paymentService.CreatePaymentAsync(paymentRequest);

            return Ok(response);
        }

        /// <summary>
        /// Tạo thanh toán MoMo
        /// </summary>
        [HttpPost("momo")]
        public async Task<IActionResult> CreateMoMoPayment([FromBody] CreatePaymentRequest request)
        {
            var paymentRequest = new PaymentRequestModel
            {
                OrderId = request.OrderId,
                Amount = request.Amount,
                PaymentMethod = PaymentMethod.MoMo,
                OrderDescription = request.Description,
                CustomerName = request.CustomerName,
                ReturnUrl = $"{Request.Scheme}://{Request.Host}/api/ExamplePayment/momo-callback",
                NotifyUrl = $"{Request.Scheme}://{Request.Host}/api/ExamplePayment/momo-notify"
            };

            var response = await _paymentService.CreatePaymentAsync(paymentRequest);

            if (response.Success)
            {
                return Ok(new
                {
                    success = true,
                    paymentUrl = response.PaymentUrl,
                    qrCodeUrl = response.QrCodeUrl,
                    orderId = response.OrderId
                });
            }

            return BadRequest(response);
        }

        /// <summary>
        /// Tạo thanh toán VNPay
        /// </summary>
        [HttpPost("vnpay")]
        public async Task<IActionResult> CreateVNPayPayment([FromBody] CreatePaymentRequest request)
        {
            var ipAddress = PaymentHelper.GetIpAddress(HttpContext);

            var paymentRequest = new PaymentRequestModel
            {
                OrderId = request.OrderId,
                Amount = request.Amount,
                PaymentMethod = PaymentMethod.VNPay,
                OrderDescription = request.Description,
                CustomerName = request.CustomerName,
                ReturnUrl = $"{Request.Scheme}://{Request.Host}/api/ExamplePayment/vnpay-callback"
            };

            var response = await _paymentService.CreatePaymentAsync(paymentRequest, ipAddress);

            if (response.Success)
            {
                return Ok(new
                {
                    success = true,
                    paymentUrl = response.PaymentUrl,
                    orderId = response.OrderId
                });
            }

            return BadRequest(response);
        }

        /// <summary>
        /// Callback từ MoMo
        /// </summary>
        [HttpGet("momo-callback")]
        public async Task<IActionResult> MoMoCallback()
        {
            var callback = new MoMoCallbackModel
            {
                PartnerCode = Request.Query["partnerCode"],
                AccessKey = Request.Query["accessKey"],
                RequestId = Request.Query["requestId"],
                Amount = Request.Query["amount"],
                OrderId = Request.Query["orderId"],
                OrderInfo = Request.Query["orderInfo"],
                OrderType = Request.Query["orderType"],
                TransId = Request.Query["transId"],
                Message = Request.Query["message"],
                LocalMessage = Request.Query["localMessage"],
                ResponseTime = Request.Query["responseTime"],
                ErrorCode = int.TryParse(Request.Query["errorCode"], out var errorCode) ? errorCode : -1,
                PayType = Request.Query["payType"],
                ExtraData = Request.Query["extraData"],
                Signature = Request.Query["signature"]
            };

            var response = await _paymentService.ProcessCallbackAsync(
                PaymentMethod.MoMo,
                callback
            );

            if (response.Success)
            {
                // TODO: Update order status in database
                // await _orderService.UpdateStatusAsync(response.OrderId, OrderStatus.Paid);

                return Redirect($"/order-success?orderId={response.OrderId}");
            }

            return Redirect($"/order-failed?orderId={response.OrderId}&error={response.Message}");
        }

        /// <summary>
        /// Notify từ MoMo (IPN)
        /// </summary>
        [HttpPost("momo-notify")]
        public async Task<IActionResult> MoMoNotify([FromBody] MoMoCallbackModel callback)
        {
            var response = await _paymentService.ProcessCallbackAsync(
                PaymentMethod.MoMo,
                callback
            );

            if (response.Success)
            {
                // TODO: Update order status in database
                // await _orderService.UpdateStatusAsync(response.OrderId, OrderStatus.Paid);

                return Ok(new { success = true });
            }

            return Ok(new { success = false, message = response.Message });
        }

        /// <summary>
        /// Callback từ VNPay
        /// </summary>
        [HttpGet("vnpay-callback")]
        public async Task<IActionResult> VNPayCallback()
        {
            var queryParams = Request.Query.ToDictionary(
                x => x.Key,
                x => x.Value.ToString()
            );

            var response = await _paymentService.ProcessCallbackAsync(
                PaymentMethod.VNPay,
                queryParams
            );

            if (response.Success)
            {
                // TODO: Update order status in database
                // await _orderService.UpdateStatusAsync(response.OrderId, OrderStatus.Paid);

                return Redirect($"/order-success?orderId={response.OrderId}");
            }

            return Redirect($"/order-failed?orderId={response.OrderId}&error={response.Message}");
        }

        /// <summary>
        /// Kiểm tra trạng thái giao dịch
        /// </summary>
        [HttpGet("status/{orderId}")]
        public async Task<IActionResult> CheckStatus(string orderId, [FromQuery] PaymentMethod method)
        {
            var response = await _paymentService.QueryTransactionAsync(method, orderId);
            return Ok(response);
        }

        /// <summary>
        /// Hoàn tiền
        /// </summary>
        [HttpPost("refund")]
        public async Task<IActionResult> Refund([FromBody] RefundRequest request)
        {
            var response = await _paymentService.RefundAsync(
                request.Method,
                request.TransactionId,
                request.Amount,
                request.Reason
            );

            return Ok(response);
        }
    }

    #region Request Models

    public class CreatePaymentRequest
    {
        public string OrderId { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerPhone { get; set; }
        public string ShippingAddress { get; set; }
    }

    public class RefundRequest
    {
        public PaymentMethod Method { get; set; }
        public string TransactionId { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; }
    }

    #endregion
}
