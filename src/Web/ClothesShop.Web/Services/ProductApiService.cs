using ClothesShop.Web.Models;
using System.Net.Http.Json;
using System.Net.Http;

namespace ClothesShop.Web.Services
{
    public interface IProductApiService
    {
        Task<List<ProductDto>> GetProductsAsync();
        Task<ProductDto?> GetProductAsync(Guid id);
        Task<ProductDto?> CreateProductAsync(CreateProductDto dto);
        Task<bool> UpdateProductAsync(Guid id, CreateProductDto dto);
        Task<bool> DeleteProductAsync(Guid id);
        Task<List<ProductDto>> GetRelatedProductsAsync(Guid id);
        Task<bool> DeductStockAsync(Guid id, int quantity);
    }

    public class ProductApiService : IProductApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthService _authService;

        public ProductApiService(IHttpClientFactory httpClientFactory, IAuthService authService)
        {
            _httpClient = httpClientFactory.CreateClient("CatalogApi");
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

        public async Task<List<ProductDto>> GetProductsAsync()
        {
            try
            {
                await AddAuthHeaderAsync();
                var products = await _httpClient.GetFromJsonAsync<List<ProductDto>>("api/products");
                return products ?? new List<ProductDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching products: {ex.Message}");
                return new List<ProductDto>();
            }
        }

        public async Task<ProductDto?> GetProductAsync(Guid id)
        {
            try
            {
                await AddAuthHeaderAsync();
                return await _httpClient.GetFromJsonAsync<ProductDto>($"api/products/{id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching product {id}: {ex.Message}");
                return null;
            }
        }

        public async Task<ProductDto?> CreateProductAsync(CreateProductDto dto)
        {
            try
            {
                await AddAuthHeaderAsync();
                var response = await _httpClient.PostAsJsonAsync("api/products", dto);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ProductDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating product: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> UpdateProductAsync(Guid id, CreateProductDto dto)
        {
            try
            {
                await AddAuthHeaderAsync();
                var response = await _httpClient.PutAsJsonAsync($"api/products/{id}", dto);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating product {id}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteProductAsync(Guid id)
        {
            try
            {
                await AddAuthHeaderAsync();
                var response = await _httpClient.DeleteAsync($"api/products/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting product {id}: {ex.Message}");
                return false;
            }
        }

        public async Task<List<ProductDto>> GetRelatedProductsAsync(Guid id)
        {
            try
            {
                await AddAuthHeaderAsync();
                var products = await _httpClient.GetFromJsonAsync<List<ProductDto>>($"api/products/{id}/related");
                return products ?? new List<ProductDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching related products for {id}: {ex.Message}");
                return new List<ProductDto>();
            }
        }

        public async Task<bool> DeductStockAsync(Guid id, int quantity)
        {
            try
            {
                await AddAuthHeaderAsync();
                var response = await _httpClient.PatchAsync($"api/products/{id}/deduct-stock?quantity={quantity}", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deducting stock for {id}: {ex.Message}");
                return false;
            }
        }
    }
}
