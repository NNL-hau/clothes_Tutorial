using Payment_Service.Models;

namespace Payment_Service.Services
{
    public interface IVNPayService
    {
        Task<PaymentResponseModel> CreatePaymentAsync(PaymentRequestModel request, string ipAddress);
        Task<PaymentResponseModel> ProcessCallbackAsync(VNPayCallbackRequest callback);
        bool ValidateSignature(Dictionary<string, string> parameters, string secureHash);
    }
}
