using Payment.API.Utils;
using Payment_Service.Configuration;
using Payment_Service.Models;
using Microsoft.Extensions.Options;

namespace Payment_Service.Services
{
    public class VNPayService : IVNPayService
    {
        private readonly VNPaySettings _settings;

        public VNPayService(
            IOptions<PaymentSettings> settings)
        {
            _settings = settings.Value.VNPay;
        }

        public async Task<PaymentResponseModel>
    CreatePaymentAsync(
        PaymentRequestModel request,
        string ipAddress)
        {
            var vnpay = new VnPayLibrary();

            // ===== VNPay dùng giờ VN (UTC+7) =====
            var vnTime =
                TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.UtcNow,
                    TimeZoneInfo.FindSystemTimeZoneById(
                        "SE Asia Standard Time"));

            string createDate =
                vnTime.ToString("yyyyMMddHHmmss");

            string txnRef = request.OrderCode;

            vnpay.AddRequestData("vnp_Version", _settings.Version);
            vnpay.AddRequestData("vnp_Command", _settings.Command);
            vnpay.AddRequestData("vnp_TmnCode", _settings.TmnCode);

            long amount = (long)(request.Amount * 100);
            vnpay.AddRequestData("vnp_Amount", amount.ToString());

            vnpay.AddRequestData(
                "vnp_CreateDate",
                vnTime.ToString("yyyyMMddHHmmss"));

            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", ipAddress);
            vnpay.AddRequestData("vnp_Locale", "vn");

            vnpay.AddRequestData(
                "vnp_OrderInfo",
                $"Thanh toan don hang {txnRef}".Trim());

            vnpay.AddRequestData("vnp_OrderType", "other");

            vnpay.AddRequestData(
                "vnp_ReturnUrl",
                _settings.ReturnUrl);

            vnpay.AddRequestData("vnp_TxnRef", txnRef);

            vnpay.AddRequestData(
                "vnp_ExpireDate",
                vnTime.AddMinutes(15)
                      .ToString("yyyyMMddHHmmss"));

            // ⚠️ Thêm bank test
            vnpay.AddRequestData("vnp_BankCode", "NCB");

            string url =
                vnpay.CreateRequestUrl(
                    _settings.PaymentUrl,
                    _settings.HashSecret);

            return new PaymentResponseModel
            {
                Success = true,
                PaymentUrl = url,
                OrderId = txnRef
            };
        }

    }
}
