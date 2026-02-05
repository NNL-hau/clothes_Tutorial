using ClothesShop.Web.Models;
using System.Net.Http.Json;
using System.Net.Http;

namespace ClothesShop.Web.Services
{
    public interface ICategoryApiService
    {
        Task<List<CategoryDto>> GetCategoriesAsync();
        Task<CategoryDto?> CreateCategoryAsync(CreateCategoryDto dto);
        Task<bool> UpdateCategoryAsync(Guid id, CreateCategoryDto dto);
        Task<bool> DeleteCategoryAsync(Guid id);
    }

    public class CategoryApiService : ICategoryApiService
    {
        private readonly HttpClient _httpClient;

        public CategoryApiService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("CatalogApi");
        }

        public async Task<List<CategoryDto>> GetCategoriesAsync()
        {
            try
            {
                var categories = await _httpClient.GetFromJsonAsync<List<CategoryDto>>("api/categories");
                return categories ?? new List<CategoryDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching categories: {ex.Message}");
                return new List<CategoryDto>();
            }
        }

        public async Task<CategoryDto?> CreateCategoryAsync(CreateCategoryDto dto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/categories", dto);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<CategoryDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating category: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> UpdateCategoryAsync(Guid id, CreateCategoryDto dto)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/categories/{id}", dto);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating category {id}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteCategoryAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/categories/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting category {id}: {ex.Message}");
                return false;
            }
        }
    }
}
