using ClothesShop.Web.Models;
using System.Net.Http.Json;
using System.Net.Http;

namespace ClothesShop.Web.Services
{
    public interface IOrderApiService
    {
        Task<List<OrderDto>> GetOrdersAsync(string? userName = null);
        Task<OrderDto?> GetOrderAsync(Guid id);
        Task<OrderDto?> CreateOrderAsync(CreateOrderRequest request);
        Task<bool> UpdateOrderStatusAsync(Guid id, string status);
        Task<bool> DeleteOrderAsync(Guid id);
    }

    public class OrderApiService : IOrderApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthService _authService;

        public OrderApiService(IHttpClientFactory httpClientFactory, IAuthService authService)
        {
            _httpClient = httpClientFactory.CreateClient("OrderingApi");
            _authService = authService;
        }

        private async Task AddAuthHeaderAsync()
        {
            var token = await _authService.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }

        public async Task<List<OrderDto>> GetOrdersAsync(string? userName = null)
        {
            try
            {
                await AddAuthHeaderAsync();
                var url = "api/orders";
                if (!string.IsNullOrEmpty(userName))
                {
                    url += $"?userName={Uri.EscapeDataString(userName)}";
                }
                var orders = await _httpClient.GetFromJsonAsync<List<OrderDto>>(url);
                return orders ?? new List<OrderDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching orders: {ex.Message}");
                return new List<OrderDto>();
            }
        }

        public async Task<OrderDto?> GetOrderAsync(Guid id)
        {
            try
            {
                await AddAuthHeaderAsync();
                return await _httpClient.GetFromJsonAsync<OrderDto>($"api/orders/{id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching order {id}: {ex.Message}");
                return null;
            }
        }

        public async Task<OrderDto?> CreateOrderAsync(CreateOrderRequest request)
        {
            try
            {
                await AddAuthHeaderAsync();
                var response = await _httpClient.PostAsJsonAsync("api/orders", request);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<OrderDto>();
                }
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error creating order: {response.StatusCode} - {error}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating order: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> UpdateOrderStatusAsync(Guid id, string status)
        {
            try
            {
                await AddAuthHeaderAsync();
                var dto = new UpdateOrderStatusDto(status);
                var response = await _httpClient.PatchAsJsonAsync($"api/orders/{id}/status", dto);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating order status {id}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteOrderAsync(Guid id)
        {
            try
            {
                await AddAuthHeaderAsync();
                var response = await _httpClient.DeleteAsync($"api/orders/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting order {id}: {ex.Message}");
                return false;
            }
        }
    }
}
