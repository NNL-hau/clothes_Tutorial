using ClothesShop.Web.Models;
using System.Net.Http.Json;
using System.Net.Http;

namespace ClothesShop.Web.Services
{
    public interface IBannerApiService
    {
        Task<List<BannerDto>> GetBannersAsync();
        Task<BannerDto?> GetBannerAsync(Guid id);
        Task<BannerDto?> CreateBannerAsync(CreateBannerDto dto);
        Task<bool> UpdateBannerAsync(Guid id, CreateBannerDto dto);
        Task<bool> DeleteBannerAsync(Guid id);
    }

    public class BannerApiService : IBannerApiService
    {
        private readonly HttpClient _httpClient;

        public BannerApiService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("CatalogApi");
        }

        public async Task<List<BannerDto>> GetBannersAsync()
        {
            try
            {
                var banners = await _httpClient.GetFromJsonAsync<List<BannerDto>>("api/banners");
                return banners ?? new List<BannerDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching banners: {ex.Message}");
                return new List<BannerDto>();
            }
        }

        public async Task<BannerDto?> GetBannerAsync(Guid id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<BannerDto>($"api/banners/{id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching banner {id}: {ex.Message}");
                return null;
            }
        }

        public async Task<BannerDto?> CreateBannerAsync(CreateBannerDto dto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/banners", dto);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<BannerDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating banner: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> UpdateBannerAsync(Guid id, CreateBannerDto dto)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/banners/{id}", dto);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating banner {id}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteBannerAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/banners/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting banner {id}: {ex.Message}");
                return false;
            }
        }
    }
}
