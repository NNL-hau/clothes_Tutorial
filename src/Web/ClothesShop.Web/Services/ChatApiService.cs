using System.Net.Http.Json;

namespace ClothesShop.Web.Services
{
    public interface IChatApiService
    {
        Task<string> GetChatResponse(string message, string? username = null);
    }

    public class ChatApiService : IChatApiService
    {
        private readonly HttpClient _httpClient;

        public ChatApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GetChatResponse(string message, string? username = null)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/Chat", new { Message = message, Username = username });
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
    }

    public record ChatResponseDto(string Response);
}
