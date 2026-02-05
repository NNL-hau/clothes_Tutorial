using ClothesShop.Web.Models;

namespace ClothesShop.Web.Services
{
    public interface IPaymentApiService
    {
        Task<List<TransactionDto>> GetTransactionsAsync();
    }
}
