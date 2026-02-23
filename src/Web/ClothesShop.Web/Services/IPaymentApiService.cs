using ClothesShop.Web.Models;

namespace ClothesShop.Web.Services
{
    public interface IPaymentApiService
    {
        /// <summary>
        /// Lấy danh sách tất cả transactions
        /// </summary>
        Task<List<TransactionDto>> GetTransactionsAsync();
        
        /// <summary>
        /// Tạo transaction mới (COD hoặc VNPay)
        /// </summary>
        /// <exception cref="HttpRequestException">Khi Payment API trả về lỗi</exception>
        Task<TransactionDto?> CreateTransactionAsync(CreateTransactionRequest request);
        
        /// <summary>
        /// Lấy thông tin một transaction cụ thể
        /// </summary>
        Task<TransactionDto?> GetTransactionAsync(Guid transactionId);
        
        /// <summary>
        /// Verify payment status sau khi callback từ VNPay
        /// </summary>
        Task<bool> VerifyPaymentStatusAsync(Guid transactionId, string expectedStatus);
        
        /// <summary>
        /// Update transaction status (DEMO MODE - no verification)
        /// </summary>
        Task<bool> UpdateTransactionStatusAsync(Guid transactionId, string status);
    }
}
