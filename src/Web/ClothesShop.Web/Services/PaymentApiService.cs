using System.Net.Http.Json;
using ClothesShop.Web.Models;

namespace ClothesShop.Web.Services
{
    public class PaymentApiService : IPaymentApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PaymentApiService>? _logger;

        public PaymentApiService(IHttpClientFactory httpClientFactory, ILogger<PaymentApiService>? logger = null)
        {
            _httpClient = httpClientFactory.CreateClient("PaymentApi");
            _logger = logger;
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
                _logger?.LogError(ex, "Error fetching transactions");
                Console.WriteLine($"Error fetching transactions: {ex.Message}");
                return new List<TransactionDto>();
            }
        }

        public async Task<TransactionDto?> CreateTransactionAsync(CreateTransactionRequest request)
        {
            try
            {
                _logger?.LogInformation("Creating transaction for Order: {OrderId}, Amount: {Amount}, Method: {Method}", 
                    request.OrderId, request.Amount, request.PaymentMethod);

                var response = await _httpClient.PostAsJsonAsync("api/transactions", request);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger?.LogError("Payment API returned error. StatusCode={StatusCode}, Body={Body}", 
                        response.StatusCode, errorBody);
                    
                    throw new HttpRequestException(
                        $"Payment API error ({response.StatusCode}): {errorBody}", 
                        null, 
                        response.StatusCode);
                }

                var transaction = await response.Content.ReadFromJsonAsync<TransactionDto>();
                
                if (transaction != null)
                {
                    _logger?.LogInformation("Transaction created successfully. TransactionId: {Id}, PaymentUrl: {Url}", 
                        transaction.Id, 
                        transaction.PaymentUrl != null ? "Generated" : "None (COD)");
                }
                
                return transaction;
            }
            catch (HttpRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Exception creating transaction for Order: {OrderId}", request.OrderId);
                Console.WriteLine($"[PaymentApiService] Exception creating transaction: {ex}");
                throw new Exception($"Không thể tạo giao dịch thanh toán: {ex.Message}", ex);
            }
        }

        public async Task<TransactionDto?> GetTransactionAsync(Guid transactionId)
        {
            try
            {
                var transactions = await GetTransactionsAsync();
                return transactions.FirstOrDefault(t => t.Id == transactionId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting transaction {TransactionId}", transactionId);
                return null;
            }
        }

        public async Task<bool> VerifyPaymentStatusAsync(Guid transactionId, string expectedStatus)
        {
            try
            {
                var transaction = await GetTransactionAsync(transactionId);
                if (transaction == null)
                {
                    _logger?.LogWarning("Transaction {TransactionId} not found for verification", transactionId);
                    return false;
                }

                var isValid = string.Equals(transaction.Status, expectedStatus, StringComparison.OrdinalIgnoreCase);
                
                _logger?.LogInformation("Payment verification for {TransactionId}: Expected={Expected}, Actual={Actual}, Valid={Valid}",
                    transactionId, expectedStatus, transaction.Status, isValid);
                
                return isValid;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error verifying payment status for {TransactionId}", transactionId);
                return false;
            }
        }
    }
}
