using System.Net.Http.Json;

namespace ClothesShop.Web.Services
{
    public interface IChatApiService
    {
        Task<string> GetChatResponse(string message, string? username = null, string? basketContext = null, string? orderContext = null, string? userProfile = null);
        Task<List<SuggestionDto>> GetSuggestions();
    }

    public class ChatApiService : IChatApiService
    {
        private readonly HttpClient _httpClient;

        public ChatApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GetChatResponse(string message, string? username = null, string? basketContext = null, string? orderContext = null, string? userProfile = null)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/Chat", new 
                { 
                    Message = message, 
                    Username = username,
                    BasketContext = basketContext,
                    OrderContext = orderContext,
                    UserProfile = userProfile
                });
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ChatResponseDto>();
                    return result?.Response ?? "Không có phản hồi từ AI.";
                }
                
                var error = await response.Content.ReadAsStringAsync();
                return $"Lỗi ({response.StatusCode}): {error}";
            }
            catch (Exception ex)
            {
                return $"Ngoại lệ: {ex.Message}";
            }
        }
        public async Task<List<SuggestionDto>> GetSuggestions()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/Chat/suggestions");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<SuggestionDto>>() ?? new List<SuggestionDto>();
                }
            }
            catch (Exception)
            {
                // Silently fail and return empty list
            }
            return new List<SuggestionDto>();
        }
    }

    public record ChatResponseDto(string Response);
    public record SuggestionDto(string Text, string Icon);
}
