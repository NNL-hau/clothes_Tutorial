using Payment.API.Utils;
using Payment_Service.Configuration;
using Payment_Service.Enums;
using Payment_Service.Models;
using Microsoft.Extensions.Options;

namespace Payment_Service.Services
{
    public class VNPayService : IVNPayService
    {
        private readonly VNPaySettings _settings;
        private readonly ILogger<VNPayService>? _logger;

        public VNPayService(IOptions<PaymentSettings> settings, ILogger<VNPayService>? logger = null)
        {
            _settings = settings.Value.VNPay;
            _logger = logger;
        }

        public async Task<PaymentResponseModel> CreatePaymentAsync(PaymentRequestModel request, string ipAddress)
        {
            try
            {
                var vnpay = new VnPayLibrary();
                var txnRef = request.OrderCode;
                var createDate = DateTime.Now.ToString("yyyyMMddHHmmss");

                // ✅ Thêm tất cả parameters theo đúng format
                vnpay.AddRequestData("vnp_Version", _settings.Version);
                vnpay.AddRequestData("vnp_Command", _settings.Command);
                vnpay.AddRequestData("vnp_TmnCode", _settings.TmnCode);

                // ✅ Amount phải nhân 100
                var amount = (long)(request.Amount * 100);
                vnpay.AddRequestData("vnp_Amount", amount.ToString());

                vnpay.AddRequestData("vnp_CreateDate", createDate);
                vnpay.AddRequestData("vnp_CurrCode", _settings.CurrCode);
                vnpay.AddRequestData("vnp_IpAddr", ipAddress);
                vnpay.AddRequestData("vnp_Locale", _settings.Locale);

                // ✅ OrderInfo: Loại bỏ ký tự đặc biệt
                var orderInfo = RemoveVietnameseTone(request.Description ?? $"Thanh toan don hang {txnRef}");
                vnpay.AddRequestData("vnp_OrderInfo", orderInfo);

                vnpay.AddRequestData("vnp_OrderType", "other");
                vnpay.AddRequestData("vnp_ReturnUrl", _settings.ReturnUrl);
                vnpay.AddRequestData("vnp_TxnRef", txnRef);

                // Optional: Thêm thời gian hết hạn (15 phút)
                var expireDate = DateTime.Now.AddMinutes(15).ToString("yyyyMMddHHmmss");
                vnpay.AddRequestData("vnp_ExpireDate", expireDate);

                // Tạo payment URL
                var paymentUrl = vnpay.CreateRequestUrl(_settings.PaymentUrl, _settings.HashSecret);

                // Log để debug
                _logger?.LogInformation("=== VNPay Payment Request ===");
                _logger?.LogInformation($"TmnCode: {_settings.TmnCode}");
                _logger?.LogInformation($"TxnRef: {txnRef}");
                _logger?.LogInformation($"Amount: {amount}");
                _logger?.LogInformation($"CreateDate: {createDate}");
                _logger?.LogInformation($"ReturnUrl: {_settings.ReturnUrl}");
                _logger?.LogInformation($"Payment URL: {paymentUrl}");

                return await Task.FromResult(new PaymentResponseModel
                {
                    Success = true,
                    Message = "Tạo thanh toán VNPay thành công",
                    PaymentUrl = paymentUrl,
                    TransactionId = txnRef,
                    OrderId = txnRef,
                    Status = PaymentStatus.Pending
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating VNPay payment");
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
            try
            {
                var vnpay = new VnPayLibrary();

                // Add all response data (VNPay sẽ trả về qua query string)
                vnpay.AddResponseData("vnp_TmnCode", callback.vnp_TmnCode ?? "");
                vnpay.AddResponseData("vnp_Amount", callback.vnp_Amount ?? "");
                vnpay.AddResponseData("vnp_BankCode", callback.vnp_BankCode ?? "");
                vnpay.AddResponseData("vnp_BankTranNo", callback.vnp_BankTranNo ?? "");
                vnpay.AddResponseData("vnp_CardType", callback.vnp_CardType ?? "");
                vnpay.AddResponseData("vnp_PayDate", callback.vnp_PayDate ?? "");
                vnpay.AddResponseData("vnp_OrderInfo", callback.vnp_OrderInfo ?? "");
                vnpay.AddResponseData("vnp_TransactionNo", callback.vnp_TransactionNo ?? "");
                vnpay.AddResponseData("vnp_ResponseCode", callback.vnp_ResponseCode ?? "");
                vnpay.AddResponseData("vnp_TransactionStatus", callback.vnp_TransactionStatus ?? "");
                vnpay.AddResponseData("vnp_TxnRef", callback.vnp_TxnRef ?? "");
                vnpay.AddResponseData("vnp_SecureHashType", callback.vnp_SecureHashType ?? "");

                // Validate signature
                bool isValidSignature = vnpay.ValidateSignature(callback.vnp_SecureHash ?? "", _settings.HashSecret);

                if (!isValidSignature)
                {
                    _logger?.LogWarning("Invalid VNPay signature");
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
                    Message = GetVNPayResponseMessage(callback.vnp_ResponseCode ?? ""),
                    TransactionId = callback.vnp_TransactionNo ?? "",
                    OrderId = callback.vnp_TxnRef ?? "",
                    Status = status
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error processing VNPay callback");
                return new PaymentResponseModel
                {
                    Success = false,
                    Message = $"Lỗi: {ex.Message}",
                    Status = PaymentStatus.Failed
                };
            }
        }

        public bool ValidateSignature(Dictionary<string, string> parameters, string secureHash)
        {
            var vnpay = new VnPayLibrary();
            foreach (var param in parameters)
            {
                vnpay.AddResponseData(param.Key, param.Value);
            }
            return vnpay.ValidateSignature(secureHash, _settings.HashSecret);
        }

        /// <summary>
        /// Loại bỏ dấu tiếng Việt để tránh lỗi encoding
        /// </summary>
        private string RemoveVietnameseTone(string text)
        {
            string[] vietnameseSigns = new string[]
            {
                "aAeEoOuUiIdDyY",
                "áàạảãâấầậẩẫăắằặẳẵ",
                "ÁÀẠẢÃÂẤẦẬẨẪĂẮẰẶẲẴ",
                "éèẹẻẽêếềệểễ",
                "ÉÈẸẺẼÊẾỀỆỂỄ",
                "óòọỏõôốồộổỗơớờợởỡ",
                "ÓÒỌỎÕÔỐỒỘỔỖƠỚỜỢỞỠ",
                "úùụủũưứừựửữ",
                "ÚÙỤỦŨƯỨỪỰỬỮ",
                "íìịỉĩ",
                "ÍÌỊỈĨ",
                "đ",
                "Đ",
                "ýỳỵỷỹ",
                "ÝỲỴỶỸ"
            };

            for (int i = 1; i < vietnameseSigns.Length; i++)
            {
                for (int j = 0; j < vietnameseSigns[i].Length; j++)
                {
                    text = text.Replace(vietnameseSigns[i][j], vietnameseSigns[0][i - 1]);
                }
            }

            return text;
        }

        private string GetVNPayResponseMessage(string responseCode)
        {
            return responseCode switch
            {
                "00" => "Giao dịch thành công",
                "07" => "Trừ tiền thành công. Giao dịch bị nghi ngờ.",
                "09" => "Thẻ/Tài khoản chưa đăng ký dịch vụ InternetBanking.",
                "10" => "Khách hàng xác thực thông tin không đúng quá 3 lần",
                "11" => "Đã hết hạn chờ thanh toán.",
                "12" => "Thẻ/Tài khoản bị khóa.",
                "13" => "Nhập sai mật khẩu xác thực giao dịch (OTP).",
                "24" => "Khách hàng hủy giao dịch",
                "51" => "Tài khoản không đủ số dư.",
                "65" => "Tài khoản đã vượt quá hạn mức giao dịch trong ngày.",
                "75" => "Ngân hàng thanh toán đang bảo trì.",
                "79" => "Nhập sai mật khẩu thanh toán quá số lần quy định.",
                _ => "Giao dịch thất bại"
            };
        }
    }
}