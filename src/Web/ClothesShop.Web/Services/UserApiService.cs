using ClothesShop.Web.Models;
using System.Net.Http.Json;
using System.Net.Http;

namespace ClothesShop.Web.Services
{
    public interface IUserApiService
    {
        Task<List<UserDto>> GetUsersAsync();
        Task<bool> UpdateUserRoleAsync(Guid id, string role);
        Task<bool> DeleteUserAsync(Guid id);
    }

    public class UserApiService : IUserApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthService _authService;

        public UserApiService(IHttpClientFactory httpClientFactory, IAuthService authService)
        {
            _httpClient = httpClientFactory.CreateClient("IdentityApi");
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

        public async Task<List<UserDto>> GetUsersAsync()
        {
            try
            {
                await AddAuthHeaderAsync();
                var users = await _httpClient.GetFromJsonAsync<List<UserDto>>("api/users");
                return users ?? new List<UserDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching users: {ex.Message}");
                return new List<UserDto>();
            }
        }

        public async Task<bool> UpdateUserRoleAsync(Guid id, string role)
        {
            try
            {
                await AddAuthHeaderAsync();
                var response = await _httpClient.PutAsJsonAsync($"api/users/{id}/role", role);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating user role {id}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteUserAsync(Guid id)
        {
            try
            {
                await AddAuthHeaderAsync();
                var response = await _httpClient.DeleteAsync($"api/users/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting user {id}: {ex.Message}");
                return false;
            }
        }
    }
}
