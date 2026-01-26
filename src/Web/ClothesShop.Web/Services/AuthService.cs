using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using ClothesShop.Web.Models;
using System.Text.Json;

namespace ClothesShop.Web.Services
{
    public interface IAuthService
    {
        Task<AuthResponse?> RegisterAsync(RegisterModel model);
        Task<AuthResponse?> LoginAsync(LoginModel model);
        Task<AuthResponse?> GoogleLoginAsync(string token);
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
                
                // Detailed error parsing
                var errorContent = await response.Content.ReadAsStringAsync();
                try 
                {
                    // Attempt to parse as JSON error object { message: "..." }
                    var doc = JsonDocument.Parse(errorContent);
                    if (doc.RootElement.TryGetProperty("message", out var msg))
                    {
                        throw new Exception(msg.GetString());
                    }
                }
                catch {}

                throw new Exception(!string.IsNullOrEmpty(errorContent) ? errorContent : "Mất kết nối tới máy chủ (500)");
            }
            catch (HttpRequestException)
            {
                throw new Exception($"Không thể kết nối tới máy chủ Backend tại {_httpClient.BaseAddress}. Hãy đảm bảo Identity.API đang chạy.");
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
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
                
                var errorContent = await response.Content.ReadAsStringAsync();
                 try 
                {
                    var doc = JsonDocument.Parse(errorContent);
                    if (doc.RootElement.TryGetProperty("message", out var msg))
                    {
                        throw new Exception(msg.GetString());
                    }
                }
                catch {}

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                     throw new Exception("Email hoặc mật khẩu không chính xác.");

                throw new Exception(!string.IsNullOrEmpty(errorContent) ? errorContent : "Lỗi đăng nhập (500)");
            }
            catch (HttpRequestException)
            {
                throw new Exception($"Không thể kết nối tới máy chủ Backend tại {_httpClient.BaseAddress}.");
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<AuthResponse?> GoogleLoginAsync(string token)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/auth/google-login", new { Token = token });
                
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
                
                throw new Exception("Google Login failed");
            }
            catch (Exception ex)
            {
                throw new Exception($"Google Login Error: {ex.Message}");
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
