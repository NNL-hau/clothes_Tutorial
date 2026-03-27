using Microsoft.EntityFrameworkCore;
using Payment_Service.Enums;
using Payment_Service.Models;
using Shopping_Tutorial.Models;
using Shopping_Tutorial.Repository;

namespace Payment_Service.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly DataContext _context;
        private readonly IMoMoService _moMoService;
        private readonly IVNPayService _vnPayService;

        public PaymentService(
            DataContext context,
            IMoMoService moMoService,
            IVNPayService vnPayService)
        {
            _context = context;
            _moMoService = moMoService;
            _vnPayService = vnPayService;
        }

        public async Task<PaymentResponseModel> CreatePaymentAsync(PaymentRequestModel request, string ipAddress = null)
        {
            try
            {
                return request.PaymentMethod switch
                {
                    PaymentMethod.COD => await ProcessCODPaymentAsync(request),
                    PaymentMethod.MoMo => await _moMoService.CreatePaymentAsync(request),
                    PaymentMethod.VNPay => await _vnPayService.CreatePaymentAsync(request, ipAddress ?? "127.0.0.1"),
                    _ => new PaymentResponseModel
                    {
                        Success = false,
                        Message = "Phương thức thanh toán không hợp lệ",
                        Status = PaymentStatus.Failed
                    }
                };
            }
            catch (Exception ex)
            {
                return new PaymentResponseModel
                {
                    Success = false,
                    Message = $"Lỗi tạo thanh toán: {ex.Message}",
                    Status = PaymentStatus.Failed
                };
            }
        }

        public async Task<PaymentResponseModel> ProcessCallbackAsync(string orderCode, Dictionary<string, string> callbackData)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderCode == orderCode);
            if (order == null)
            {
                return new PaymentResponseModel
                {
                    Success = false,
                    Message = "Không tìm thấy đơn hàng",
                    Status = PaymentStatus.Failed
                };
            }

            PaymentResponseModel result;

            if (callbackData.ContainsKey("partnerCode")) // MoMo callback
            {
                var momoCallback = MapToMoMoCallback(callbackData);
                result = await _moMoService.ProcessCallbackAsync(momoCallback);
            }
            else if (callbackData.ContainsKey("vnp_TmnCode")) // VNPay callback
            {
                var vnpayCallback = MapToVNPayCallback(callbackData);
                result = await _vnPayService.ProcessCallbackAsync(vnpayCallback);
            }
            else
            {
                return new PaymentResponseModel
                {
                    Success = false,
                    Message = "Callback không hợp lệ",
                    Status = PaymentStatus.Failed
                };
            }

            await UpdateOrderPaymentStatusAsync(orderCode, result);
            return result;
        }

        public async Task<bool> UpdateOrderPaymentStatusAsync(string orderCode, PaymentResponseModel paymentResult)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderCode == orderCode);
            if (order == null) return false;

            order.PaymentStatus = (int)paymentResult.Status;

            if (paymentResult.Success)
            {
                order.Status = (int)OrderStatus.Processing;
                order.PaidDate = DateTime.Now;
                order.TransactionId = paymentResult.TransactionId;
            }
            else if (paymentResult.Status == PaymentStatus.Failed)
            {
                order.Status = (int)OrderStatus.Cancelled;
            }

            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<OrderPaymentModel> GetOrderByCodeAsync(string orderCode)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderCode == orderCode);
            if (order == null) return null;

            // Mapping tối thiểu để không làm hỏng compile/runtime.
            return new OrderPaymentModel
            {
                OrderCode = order.OrderCode,
                UserName = order.UserName,
                TotalAmount = order.TotalAmount,
                ShippingCost = order.ShippingCost,
                CouponAmount = order.CouponAmount,
                PaymentMethod = (PaymentMethod)order.PaymentMethod,
                PaymentStatus = (PaymentStatus)order.PaymentStatus,
                OrderStatus = (OrderStatus)order.Status,
                TransactionId = order.TransactionId,
                CreatedDate = order.CreatedDate,
                PaidDate = order.PaidDate,
                CustomerName = order.CustomerName,
                CustomerEmail = order.CustomerEmail,
                CustomerPhone = order.CustomerPhone,
                ShippingAddress = order.ShippingAddress,
                Note = order.Note
            };
        }

        private Task<PaymentResponseModel> ProcessCODPaymentAsync(PaymentRequestModel request)
        {
            // COD: không tạo link thanh toán, thường chỉ tạo đơn hàng và chờ giao hàng.
            return Task.FromResult(new PaymentResponseModel
            {
                Success = true,
                Message = "Đã tạo đơn COD. Vui lòng thanh toán khi nhận hàng.",
                Status = PaymentStatus.Pending,
                TransactionId = $"COD-{request.OrderCode}-{DateTime.UtcNow.Ticks}",
                PaymentUrl = string.Empty,
                Data = new Dictionary<string, string>()
            });
        }

        private static MoMoCallbackRequest MapToMoMoCallback(Dictionary<string, string> callbackData)
        {
            long.TryParse(GetValue(callbackData, "amount"), out var amount);
            long.TryParse(GetValue(callbackData, "transId"), out var transId);
            long.TryParse(GetValue(callbackData, "responseTime"), out var responseTime);
            int.TryParse(GetValue(callbackData, "resultCode"), out var resultCode);

            return new MoMoCallbackRequest
            {
                PartnerCode = GetValue(callbackData, "partnerCode"),
                OrderId = GetValue(callbackData, "orderId"),
                RequestId = GetValue(callbackData, "requestId"),
                Amount = amount,
                OrderInfo = GetValue(callbackData, "orderInfo"),
                OrderType = GetValue(callbackData, "orderType"),
                TransId = transId,
                ResultCode = resultCode,
                Message = GetValue(callbackData, "message"),
                PayType = GetValue(callbackData, "payType"),
                ResponseTime = responseTime,
                ExtraData = GetValue(callbackData, "extraData"),
                Signature = GetValue(callbackData, "signature")
            };
        }

        private static VNPayCallbackRequest MapToVNPayCallback(Dictionary<string, string> callbackData)
        {
            return new VNPayCallbackRequest
            {
                vnp_TmnCode = GetValue(callbackData, "vnp_TmnCode"),
                vnp_Amount = GetValue(callbackData, "vnp_Amount"),
                vnp_BankCode = GetValue(callbackData, "vnp_BankCode"),
                vnp_BankTranNo = GetValue(callbackData, "vnp_BankTranNo"),
                vnp_CardType = GetValue(callbackData, "vnp_CardType"),
                vnp_PayDate = GetValue(callbackData, "vnp_PayDate"),
                vnp_OrderInfo = GetValue(callbackData, "vnp_OrderInfo"),
                vnp_TransactionNo = GetValue(callbackData, "vnp_TransactionNo"),
                vnp_ResponseCode = GetValue(callbackData, "vnp_ResponseCode"),
                vnp_TransactionStatus = GetValue(callbackData, "vnp_TransactionStatus"),
                vnp_TxnRef = GetValue(callbackData, "vnp_TxnRef"),
                vnp_SecureHashType = GetValue(callbackData, "vnp_SecureHashType"),
                vnp_SecureHash = GetValue(callbackData, "vnp_SecureHash")
            };
        }

        private static string GetValue(Dictionary<string, string> dict, string key)
            => dict.TryGetValue(key, out var value) ? value : string.Empty;
    }
}






