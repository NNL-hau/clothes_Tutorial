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
    }

    public class ProductApiService : IProductApiService
    {
        private readonly HttpClient _httpClient;

        public ProductApiService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("CatalogApi");
        }

        public async Task<List<ProductDto>> GetProductsAsync()
        {
            try
            {
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
                var response = await _httpClient.DeleteAsync($"api/products/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting product {id}: {ex.Message}");
                return false;
            }
        }
    }
}
