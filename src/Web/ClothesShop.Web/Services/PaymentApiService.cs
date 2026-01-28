using System.Net.Http.Json;
using ClothesShop.Web.Models;

namespace ClothesShop.Web.Services
{
    public class PaymentApiService : IPaymentApiService
    {
        private readonly HttpClient _httpClient;

        public PaymentApiService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("PaymentApi");
        }

        public async Task<List<TransactionDto>> GetTransactionsAsync()
        {
            try
            {
                var transactions = await _httpClient.GetFromJsonAsync<List<TransactionDto>>("api/transactions");
                return transactions ?? new List<TransactionDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching transactions: {ex.Message}");
                return new List<TransactionDto>();
            }
        }
    }
}
