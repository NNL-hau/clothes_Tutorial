using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using ClothesShop.Web.Models;
using System.Text.Json;

namespace ClothesShop.Web.Services
{
    public interface IAuthService
    {
        event Action OnAuthStateChanged;
        UserInfo? CurrentUser { get; }
        Task InitializeAsync();
        Task<AuthResponse?> RegisterAsync(RegisterModel model);
        Task<AuthResponse?> LoginAsync(LoginModel model);
        Task<AuthResponse?> GoogleLoginAsync(string token);
        Task LogoutAsync();
        Task<UserInfo?> GetCurrentUserAsync();
        Task<bool> IsAuthenticatedAsync();
        Task<string?> GetTokenAsync();
        Task<string?> GetUserRoleAsync();
        Task<string?> GetUserNameAsync();
        Task<bool> SendOtpAsync(string username, string email);
        Task<bool> VerifyOtpAsync(string email, string otpCode);
        Task<bool> ResetPasswordAsync(string email, string otpCode, string newPassword);
    }
    
    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;
        private const string TOKEN_KEY = "authToken";
        private const string USER_KEY = "currentUser";
        public event Action? OnAuthStateChanged;
        public UserInfo? CurrentUser { get; private set; }
        
        public AuthService(IHttpClientFactory httpClientFactory, ILocalStorageService localStorage)
        {
            _httpClient = httpClientFactory.CreateClient("IdentityApi");
            _localStorage = localStorage;
        }

        public async Task InitializeAsync()
        {
            CurrentUser = await GetCurrentUserAsync();
            if (CurrentUser != null)
            {
                OnAuthStateChanged?.Invoke();
            }
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
                        CurrentUser = await GetCurrentUserAsync();
                        OnAuthStateChanged?.Invoke();
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
                        CurrentUser = await GetCurrentUserAsync();
                        OnAuthStateChanged?.Invoke();
                    }
                    return authResponse;
                }
                
                var errorContent = await response.Content.ReadAsStringAsync();
                try 
                {
                    using var doc = JsonDocument.Parse(errorContent);
                    var root = doc.RootElement;
                    
                    // Priority 1: Direct "message" property
                    if (root.TryGetProperty("message", out var msg))
                    {
                        throw new Exception(msg.GetString());
                    }

                    // Priority 2: Standard ASP.NET "errors" property
                    if (root.TryGetProperty("errors", out var errors))
                    {
                        var errorMessages = new List<string>();
                        foreach (var error in errors.EnumerateObject())
                        {
                            foreach (var detail in error.Value.EnumerateArray())
                            {
                                errorMessages.Add(detail.GetString() ?? "");
                            }
                        }
                        if (errorMessages.Any())
                        {
                            throw new Exception(string.Join(" ", errorMessages));
                        }
                    }
                    
                    // Priority 3: Common "title" property in ProblemDetails
                    if (root.TryGetProperty("title", out var title))
                    {
                         throw new Exception(title.GetString());
                    }
                }
                catch (Exception ex) when (!(ex is Exception && ex.Source == null)) // Don't catch our own custom exceptions
                {
                    if (ex.Message != null && ex.Message.Length > 0 && !ex.Message.Contains("JSON"))
                        throw;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                     throw new Exception("Tên đăng nhập/Email hoặc mật khẩu không chính xác.");

                throw new Exception("Lỗi đăng nhập. Vui lòng thử lại.");
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
                        OnAuthStateChanged?.Invoke();
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
            CurrentUser = null;
            OnAuthStateChanged?.Invoke();
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

        public async Task<string?> GetUserRoleAsync()
        {
            var user = await GetCurrentUserAsync();
            return user?.Role;
        }

        public async Task<string?> GetUserNameAsync()
        {
            var user = await GetCurrentUserAsync();
            return user?.FullName ?? user?.Email;
        }
        
        public async Task<bool> SendOtpAsync(string username, string email)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/auth/forgot-password", new { Username = username, Email = email });
                
                if (response.IsSuccessStatusCode)
                {
                    return true;
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
                catch { }
                
                throw new Exception(!string.IsNullOrEmpty(errorContent) ? errorContent : "Không thể gửi mã OTP");
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
        
        public async Task<bool> VerifyOtpAsync(string email, string otpCode)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/auth/verify-otp", new { Email = email, OtpCode = otpCode });
                
                if (response.IsSuccessStatusCode)
                {
                    return true;
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
                catch { }
                
                throw new Exception(!string.IsNullOrEmpty(errorContent) ? errorContent : "Mã OTP không hợp lệ");
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
        
        public async Task<bool> ResetPasswordAsync(string email, string otpCode, string newPassword)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/auth/reset-password", new 
                { 
                    Email = email, 
                    OtpCode = otpCode, 
                    NewPassword = newPassword 
                });
                
                if (response.IsSuccessStatusCode)
                {
                    return true;
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
                catch { }
                
                throw new Exception(!string.IsNullOrEmpty(errorContent) ? errorContent : "Không thể đặt lại mật khẩu");
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
    }
}
