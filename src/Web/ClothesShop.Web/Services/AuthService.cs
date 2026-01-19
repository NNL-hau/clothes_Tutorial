using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using ClothesShop.Web.Models;

namespace ClothesShop.Web.Services
{
    public interface IAuthService
    {
        Task<AuthResponse?> RegisterAsync(RegisterModel model);
        Task<AuthResponse?> LoginAsync(LoginModel model);
        Task LogoutAsync();
        Task<UserInfo?> GetCurrentUserAsync();
        Task<bool> IsAuthenticatedAsync();
        Task<string?> GetTokenAsync();
    }
    
    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;
        private const string TOKEN_KEY = "authToken";
        private const string USER_KEY = "currentUser";
        
        public AuthService(HttpClient httpClient, ILocalStorageService localStorage)
        {
            _httpClient = httpClient;
            _localStorage = localStorage;
        }
        
        public async Task<AuthResponse?> RegisterAsync(RegisterModel model)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/auth/register", model);
                
                if (response.IsSuccessStatusCode)
                {
                    var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
                    if (authResponse != null)
                    {
                        await _localStorage.SetItemAsStringAsync(TOKEN_KEY, authResponse.Token);
                        _httpClient.DefaultRequestHeaders.Authorization = 
                            new AuthenticationHeaderValue("Bearer", authResponse.Token);
                    }
                    return authResponse;
                }
                
                return null;
            }
            catch
            {
                return null;
            }
        }
        
        public async Task<AuthResponse?> LoginAsync(LoginModel model)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/auth/login", model);
                
                if (response.IsSuccessStatusCode)
                {
                    var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
                    if (authResponse != null)
                    {
                        await _localStorage.SetItemAsStringAsync(TOKEN_KEY, authResponse.Token);
                        _httpClient.DefaultRequestHeaders.Authorization = 
                            new AuthenticationHeaderValue("Bearer", authResponse.Token);
                    }
                    return authResponse;
                }
                
                return null;
            }
            catch
            {
                return null;
            }
        }
        
        public async Task LogoutAsync()
        {
            await _localStorage.RemoveItemAsync(TOKEN_KEY);
            await _localStorage.RemoveItemAsync(USER_KEY);
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
        
        public async Task<UserInfo?> GetCurrentUserAsync()
        {
            try
            {
                var token = await GetTokenAsync();
                if (string.IsNullOrEmpty(token))
                    return null;
                
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", token);
                
                return await _httpClient.GetFromJsonAsync<UserInfo>("api/auth/me");
            }
            catch
            {
                return null;
            }
        }
        
        public async Task<bool> IsAuthenticatedAsync()
        {
            var token = await GetTokenAsync();
            return !string.IsNullOrEmpty(token);
        }
        
        public async Task<string?> GetTokenAsync()
        {
            return await _localStorage.GetItemAsStringAsync(TOKEN_KEY);
        }
    }
}
