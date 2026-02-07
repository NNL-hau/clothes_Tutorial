using Microsoft.Extensions.Options;
using Payment_Service.Configuration;
using Payment_Service.Enums;
using Payment_Service.Helpers;
using Payment_Service.Models;
using System.Text;
using System.Text.Json;

namespace Payment_Service.Services
{
    public class MoMoService : IMoMoService
    {
        private readonly MoMoSettings _settings;
        private readonly HttpClient _httpClient;

        public MoMoService(IOptions<PaymentSettings> settings, IHttpClientFactory httpClientFactory)
        {
            _settings = settings.Value.MoMo;
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<PaymentResponseModel> CreatePaymentAsync(PaymentRequestModel request)
        {
            try
            {
                var requestId = SecurityHelper.GenerateRequestId();
                var orderId = request.OrderCode;
                var amount = ((long)request.Amount).ToString();
                var orderInfo = request.Description ?? $"Thanh toán đơn hàng {orderId}";
                var extraData = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                    JsonSerializer.Serialize(request.ExtraData ?? new Dictionary<string, string>())
                ));

                // Tạo chữ ký
                var rawHash = $"accessKey={_settings.AccessKey}" +
                             $"&amount={amount}" +
                             $"&extraData={extraData}" +
                             $"&ipnUrl={_settings.NotifyUrl}" +
                             $"&orderId={orderId}" +
                             $"&orderInfo={orderInfo}" +
                             $"&partnerCode={_settings.PartnerCode}" +
                             $"&redirectUrl={_settings.ReturnUrl}" +
                             $"&requestId={requestId}" +
                             $"&requestType=captureWallet";

                var signature = SecurityHelper.HmacSHA256(rawHash, _settings.SecretKey);

                var momoRequest = new MoMoPaymentRequest
                {
                    PartnerCode = _settings.PartnerCode,
                    AccessKey = _settings.AccessKey,
                    RequestId = requestId,
                    Amount = amount,
                    OrderId = orderId,
                    OrderInfo = orderInfo,
                    RedirectUrl = _settings.ReturnUrl,
                    IpnUrl = _settings.NotifyUrl,
                    ExtraData = extraData,
                    RequestType = "captureWallet",
                    Signature = signature,
                    Lang = "vi"
                };

                var jsonContent = JsonSerializer.Serialize(momoRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_settings.PaymentUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                var momoResponse = JsonSerializer.Deserialize<MoMoPaymentResponse>(responseContent);

                if (momoResponse?.ResultCode == 0)
                {
                    return new PaymentResponseModel
                    {
                        Success = true,
                        Message = "Tạo thanh toán MoMo thành công",
                        PaymentUrl = momoResponse.PayUrl,
                        TransactionId = momoResponse.RequestId,
                        Status = PaymentStatus.Pending,
                        Data = new Dictionary<string, string>
                        {
                            { "OrderId", momoResponse.OrderId },
                            { "RequestId", momoResponse.RequestId },
                            { "QrCodeUrl", momoResponse.QrCodeUrl },
                            { "Deeplink", momoResponse.Deeplink }
                        }
                    };
                }

                return new PaymentResponseModel
                {
                    Success = false,
                    Message = momoResponse?.Message ?? "Tạo thanh toán thất bại",
                    Status = PaymentStatus.Failed
                };
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

        public async Task<PaymentResponseModel> ProcessCallbackAsync(MoMoCallbackRequest callback)
        {
            if (!ValidateSignature(callback))
            {
                return new PaymentResponseModel
                {
                    Success = false,
                    Message = "Chữ ký không hợp lệ",
                    Status = PaymentStatus.Failed
                };
            }

            var status = callback.ResultCode == 0 ? PaymentStatus.Success : PaymentStatus.Failed;

            return new PaymentResponseModel
            {
                Success = callback.ResultCode == 0,
                Message = callback.Message,
                TransactionId = callback.TransId.ToString(),
                Status = status,
                Data = new Dictionary<string, string>
                {
                    { "OrderId", callback.OrderId },
                    { "RequestId", callback.RequestId },
                    { "Amount", callback.Amount.ToString() },
                    { "PayType", callback.PayType }
                }
            };
        }

        public bool ValidateSignature(MoMoCallbackRequest callback)
        {
            var rawHash = $"accessKey={_settings.AccessKey}" +
                         $"&amount={callback.Amount}" +
                         $"&extraData={callback.ExtraData}" +
                         $"&message={callback.Message}" +
                         $"&orderId={callback.OrderId}" +
                         $"&orderInfo={callback.OrderInfo}" +
                         $"&orderType={callback.OrderType}" +
                         $"&partnerCode={callback.PartnerCode}" +
                         $"&payType={callback.PayType}" +
                         $"&requestId={callback.RequestId}" +
                         $"&responseTime={callback.ResponseTime}" +
                         $"&resultCode={callback.ResultCode}" +
                         $"&transId={callback.TransId}";

            var signature = SecurityHelper.HmacSHA256(rawHash, _settings.SecretKey);
            return signature.Equals(callback.Signature, StringComparison.OrdinalIgnoreCase);
        }
    }
}