using Microsoft.Extensions.Options;
using Payment_Service.Configuration;
using Payment_Service.Enums;
using Payment_Service.Helpers;
using Payment_Service.Models;

namespace Payment_Service.Services
{
    public class VNPayService : IVNPayService
    {
        private readonly VNPaySettings _settings;

        public VNPayService(IOptions<PaymentSettings> settings)
        {
            _settings = settings.Value.VNPay;
        }

        public async Task<PaymentResponseModel> CreatePaymentAsync(PaymentRequestModel request, string ipAddress)
        {
            try
            {
                var parameters = new Dictionary<string, string>
                {
                    { "vnp_Version", _settings.Version },
                    { "vnp_Command", _settings.Command },
                    { "vnp_TmnCode", _settings.TmnCode },
                    { "vnp_Amount", PaymentHelper.FormatAmount(request.Amount) },
                    { "vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss") },
                    { "vnp_CurrCode", _settings.CurrCode },
                    { "vnp_IpAddr", ipAddress },
                    { "vnp_Locale", _settings.Locale },
                    { "vnp_OrderInfo", request.Description ?? $"Thanh toán đơn hàng {request.OrderCode}" },
                    { "vnp_OrderType", "other" },
                    { "vnp_ReturnUrl", _settings.ReturnUrl },
                    { "vnp_TxnRef", request.OrderCode }
                };

                // Tạo query string và chữ ký
                var queryString = PaymentHelper.BuildQueryString(parameters);
                var signData = queryString;
                var secureHash = SecurityHelper.HmacSHA256(signData, _settings.HashSecret);

                var paymentUrl = $"{_settings.PaymentUrl}?{queryString}&vnp_SecureHash={secureHash}";

                return await Task.FromResult(new PaymentResponseModel
                {
                    Success = true,
                    Message = "Tạo thanh toán VNPay thành công",
                    PaymentUrl = paymentUrl,
                    TransactionId = request.OrderCode,
                    Status = PaymentStatus.Pending
                });
            }
            catch (Exception ex)
            {
                return new PaymentResponseModel
                {
                    Success = false,
                    Message = $"Lỗi: {ex.Message}",
                    Status = PaymentStatus.Failed
                };
            }
        }

        public async Task<PaymentResponseModel> ProcessCallbackAsync(VNPayCallbackRequest callback)
        {
            var parameters = new Dictionary<string, string>
            {
                { "vnp_TmnCode", callback.vnp_TmnCode },
                { "vnp_Amount", callback.vnp_Amount },
                { "vnp_BankCode", callback.vnp_BankCode },
                { "vnp_BankTranNo", callback.vnp_BankTranNo },
                { "vnp_CardType", callback.vnp_CardType },
                { "vnp_PayDate", callback.vnp_PayDate },
                { "vnp_OrderInfo", callback.vnp_OrderInfo },
                { "vnp_TransactionNo", callback.vnp_TransactionNo },
                { "vnp_ResponseCode", callback.vnp_ResponseCode },
                { "vnp_TransactionStatus", callback.vnp_TransactionStatus },
                { "vnp_TxnRef", callback.vnp_TxnRef },
                { "vnp_SecureHashType", callback.vnp_SecureHashType }
            };

            if (!ValidateSignature(parameters, callback.vnp_SecureHash))
            {
                return new PaymentResponseModel
                {
                    Success = false,
                    Message = "Chữ ký không hợp lệ",
                    Status = PaymentStatus.Failed
                };
            }

            var isSuccess = callback.vnp_ResponseCode == "00" && callback.vnp_TransactionStatus == "00";
            var status = isSuccess ? PaymentStatus.Success : PaymentStatus.Failed;

            return await Task.FromResult(new PaymentResponseModel
            {
                Success = isSuccess,
                Message = PaymentHelper.GetVNPayResponseMessage(callback.vnp_ResponseCode),
                TransactionId = callback.vnp_TransactionNo,
                Status = status,
                Data = new Dictionary<string, string>
                {
                    { "OrderCode", callback.vnp_TxnRef },
                    { "Amount", callback.vnp_Amount },
                    { "BankCode", callback.vnp_BankCode },
                    { "CardType", callback.vnp_CardType }
                }
            });
        }

        public bool ValidateSignature(Dictionary<string, string> parameters, string secureHash)
        {
            var queryString = PaymentHelper.BuildQueryString(parameters);
            var calculatedHash = SecurityHelper.HmacSHA256(queryString, _settings.HashSecret);
            return calculatedHash.Equals(secureHash, StringComparison.OrdinalIgnoreCase);
        }
    }
}
