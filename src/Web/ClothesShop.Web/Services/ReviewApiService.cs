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

        public ReviewApiService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ReviewApi");
        }

        public async Task<List<ReviewDto>> GetReviewsAsync()
        {
            try
            {
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
