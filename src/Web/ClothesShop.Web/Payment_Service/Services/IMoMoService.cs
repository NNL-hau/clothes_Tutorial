using Payment_Service.Models;

namespace Payment_Service.Services
{
    public interface IMoMoService
    {
        Task<PaymentResponseModel> CreatePaymentAsync(PaymentRequestModel request);
        Task<PaymentResponseModel> ProcessCallbackAsync(MoMoCallbackRequest callback);
        bool ValidateSignature(MoMoCallbackRequest callback);
    }
}