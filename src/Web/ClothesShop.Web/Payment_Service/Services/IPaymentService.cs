using Payment_Service.Models;

namespace Payment_Service.Services
{
    public interface IPaymentService
    {
        Task<PaymentResponseModel> CreatePaymentAsync(PaymentRequestModel request, string ipAddress = null);
        Task<PaymentResponseModel> ProcessCallbackAsync(string orderCode, Dictionary<string, string> callbackData);
        Task<bool> UpdateOrderPaymentStatusAsync(string orderCode, PaymentResponseModel paymentResult);
        Task<OrderPaymentModel> GetOrderByCodeAsync(string orderCode);
    }
}
