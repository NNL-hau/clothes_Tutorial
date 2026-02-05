using Google.Apis.Auth;

namespace Identity.API.Services
{
    public interface IGoogleAuthService
    {
        Task<GoogleJsonWebSignature.Payload?> VerifyGoogleTokenAsync(string token);
    }

    public class GoogleAuthService : IGoogleAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<GoogleAuthService> _logger;

        public GoogleAuthService(IConfiguration configuration, ILogger<GoogleAuthService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<GoogleJsonWebSignature.Payload?> VerifyGoogleTokenAsync(string token)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings()
                {
                    Audience = new List<string> { _configuration["Google:ClientId"] ?? "" }
                };
                
                var payload = await GoogleJsonWebSignature.ValidateAsync(token, settings);
                return payload;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to verify Google token");
                return null;
            }
        }
    }
}
