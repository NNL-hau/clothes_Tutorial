using System.Net.Http.Json;

namespace ClothesShop.Web.Services
{
    public interface ICouponApiService
    {
        Task<List<CouponDto>> GetAllAsync();
        Task<List<CouponDto>> GetActiveAsync();
        Task<CouponValidateResult?> ValidateAsync(string code, decimal orderAmount);
        Task<CouponDto?> CreateAsync(CreateCouponRequest dto);
        Task<bool> UpdateAsync(Guid id, CreateCouponRequest dto);
        Task<bool> DeleteAsync(Guid id);
    }

    public class CouponApiService : ICouponApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthService _authService;

        public CouponApiService(IHttpClientFactory httpClientFactory, IAuthService authService)
        {
            _httpClient = httpClientFactory.CreateClient("OrderingApi");
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

        public async Task<List<CouponDto>> GetAllAsync()
        {
            try
            {
                await AddAuthHeaderAsync();
                var coupons = await _httpClient.GetFromJsonAsync<List<CouponDto>>("api/coupons");
                return coupons ?? new List<CouponDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching all coupons: {ex.Message}");
                return new List<CouponDto>();
            }
        }

        public async Task<List<CouponDto>> GetActiveAsync()
        {
            try
            {
                // Active coupons are usually public for users to see or validate, might not need auth if we allow guest validation
                // But let's add auth header just in case.
                var coupons = await _httpClient.GetFromJsonAsync<List<CouponDto>>("api/coupons/active");
                return coupons ?? new List<CouponDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching active coupons: {ex.Message}");
                return new List<CouponDto>();
            }
        }

        public async Task<CouponValidateResult?> ValidateAsync(string code, decimal orderAmount)
        {
            try
            {
                var request = new { Code = code, OrderAmount = orderAmount };
                var response = await _httpClient.PostAsJsonAsync("api/coupons/validate", request);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<CouponValidateResult>();
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error validating coupon: {ex.Message}");
                return null;
            }
        }

        public async Task<CouponDto?> CreateAsync(CreateCouponRequest dto)
        {
            try
            {
                await AddAuthHeaderAsync();
                var response = await _httpClient.PostAsJsonAsync("api/coupons", dto);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<CouponDto>();
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating coupon: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> UpdateAsync(Guid id, CreateCouponRequest dto)
        {
            try
            {
                await AddAuthHeaderAsync();
                var response = await _httpClient.PutAsJsonAsync($"api/coupons/{id}", dto);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating coupon: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                await AddAuthHeaderAsync();
                var response = await _httpClient.DeleteAsync($"api/coupons/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting coupon: {ex.Message}");
                return false;
            }
        }
    }

    public record CouponDto(
        Guid Id, string Code, string Description, string DiscountType,
        decimal DiscountValue, decimal? MaxDiscount, decimal MinOrderAmount,
        int? UsageLimit, int UsedCount, bool IsActive,
        DateTime? ExpiryDate, DateTime CreatedAt);

    public record CreateCouponRequest(
        string Code, string Description, string DiscountType,
        decimal DiscountValue, decimal? MaxDiscount, decimal MinOrderAmount,
        int? UsageLimit, bool IsActive, DateTime? ExpiryDate);

    public record CouponValidateResult(
        bool Success, string Message, decimal DiscountAmount, 
        Guid? CouponId, string? Code, string? Description);
}
