using ClothesShop.Web.Models;
using System.Net.Http.Json;
using System.Net.Http;

namespace ClothesShop.Web.Services
{
    public interface IReviewApiService
    {
        Task<List<ReviewDto>> GetReviewsAsync();
        Task<bool> ApproveReviewAsync(Guid id, bool approve);
        Task<bool> DeleteReviewAsync(Guid id);
    }

    public class ReviewApiService : IReviewApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthService _authService;

        public ReviewApiService(IHttpClientFactory httpClientFactory, IAuthService authService)
        {
            _httpClient = httpClientFactory.CreateClient("ReviewApi");
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

        public async Task<List<ReviewDto>> GetReviewsAsync()
        {
            try
            {
                await AddAuthHeaderAsync();
                return await _httpClient.GetFromJsonAsync<List<ReviewDto>>("api/reviews") ?? new();
            }
            catch
            {
                return new();
            }
        }

        public async Task<bool> ApproveReviewAsync(Guid id, bool approve)
        {
            try
            {
                await AddAuthHeaderAsync();
                var response = await _httpClient.PatchAsJsonAsync($"api/reviews/{id}/approve", approve);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteReviewAsync(Guid id)
        {
            try
            {
                await AddAuthHeaderAsync();
                var response = await _httpClient.DeleteAsync($"api/reviews/{id}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
