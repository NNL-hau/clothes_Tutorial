using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Identity.API.Data;
using Identity.API.DTOs;
using Identity.API.Models;
using Identity.API.Services;
using BCrypt.Net;

namespace Identity.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IdentityDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly IGoogleAuthService _googleAuthService;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthController> _logger;
        
        public AuthController(
            IdentityDbContext context,
            IJwtService jwtService,
            IGoogleAuthService googleAuthService,
            IEmailService emailService,
            ILogger<AuthController> logger)
        {
            _context = context;
            _jwtService = jwtService;
            _googleAuthService = googleAuthService;
            _emailService = emailService;
            _logger = logger;
        }
        
        /// <summary>
        /// Register a new customer
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            try
            {
                // Check if user already exists
                // Check if user already exists (Email or FullName)
                if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                {
                    return BadRequest(new { message = "Email đã tồn tại" });
                }

                if (await _context.Users.AnyAsync(u => u.FullName == request.FullName))
                {
                    return BadRequest(new { message = "Tên đăng nhập đã tồn tại" });
                }
                
                // Create new user
                var user = new User
                {
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    FullName = request.FullName,
                    PhoneNumber = request.PhoneNumber,
                    Address = request.Address,
                    Role = "Customer" // Default role
                };
                
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                
                // Generate JWT token
                var token = _jwtService.GenerateToken(user);
                
                return Ok(new AuthResponse
                {
                    UserId = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    Role = user.Role,
                    Token = token
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                return StatusCode(500, new { message = "An error occurred during registration" });
            }
        }
        
        /// <summary>
        /// Login with email and password
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                // Find user by email or username (FullName)
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.UserIdentifier || u.FullName == request.UserIdentifier);
                
                if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    return Unauthorized(new { message = "Tên đăng nhập hoặc mật khẩu không chính xác." });
                }
                
                // Generate JWT token
                var token = _jwtService.GenerateToken(user);
                
                return Ok(new AuthResponse
                {
                    UserId = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    Role = user.Role,
                    Token = token
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, new { message = "An error occurred during login" });
            }
        }
        
        /// <summary>
        /// Get current user information (requires authentication)
        /// </summary>
        [HttpGet("me")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<ActionResult<UserResponse>> GetCurrentUser()
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized();
                }
                
                var user = await _context.Users.FindAsync(userId);
                
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }
                
                return Ok(new UserResponse
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    Role = user.Role,
                    PhoneNumber = user.PhoneNumber,
                    Address = user.Address,
                    CreatedAt = user.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Login with Google
        /// </summary>
        [HttpPost("google-login")]
        public async Task<ActionResult<AuthResponse>> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            try
            {
                var payload = await _googleAuthService.VerifyGoogleTokenAsync(request.Token);
                if (payload == null)
                {
                    return BadRequest(new { message = "Invalid Google Token" });
                }

                // Check if user exists
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == payload.Email);
                if (user == null)
                {
                    // Create new user
                    user = new User
                    {
                        Email = payload.Email,
                        FullName = payload.Name,
                        PasswordHash = "", // No password for Google users
                        Role = "Customer",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                }

                // Generate JWT token
                var token = _jwtService.GenerateToken(user);

                return Ok(new AuthResponse
                {
                    UserId = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    Role = user.Role,
                    Token = token
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Google login");
                return StatusCode(500, new { message = "An error occurred during Google login" });
            }
        }
        /// <summary>
        /// Update user profile (requires authentication)
        /// </summary>
        [HttpPut("profile")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<ActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized();
                }
                
                var user = await _context.Users.FindAsync(userId);
                
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Update phone and address
                if (request.PhoneNumber != null) user.PhoneNumber = request.PhoneNumber;
                if (request.Address != null) user.Address = request.Address;

                // Update password if provided
                if (!string.IsNullOrEmpty(request.NewPassword))
                {
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                }

                await _context.SaveChangesAsync();
                
                return Ok(new { message = "Cập nhật thông tin thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile");
                return StatusCode(500, new { message = "An error occurred during profile update" });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try 
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.FullName == request.Username);
                if (user == null)
                {
                    // For security, don't reveal if user exists. 
                    // But in a tutorial context, we might want to be more explicit.
                    return BadRequest(new { message = "Thông tin không khớp với bất kỳ tài khoản nào." });
                }

                // Generate 6-digit OTP
                var otp = new Random().Next(100000, 999999).ToString();
                user.ResetOtp = otp;
                user.ResetOtpExpiry = DateTime.UtcNow.AddMinutes(10);

                await _context.SaveChangesAsync();

                // Send Email
                await _emailService.SendOtpEmailAsync(user.Email, otp);

                return Ok(new { message = "Mã OTP đã được gửi đến email của bạn." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during forgot-password");
                return StatusCode(500, new { message = "Lỗi hệ thống khi gửi OTP" });
            }
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            try 
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                if (user == null || user.ResetOtp != request.OtpCode || (user.ResetOtpExpiry != null && user.ResetOtpExpiry < DateTime.UtcNow))
                {
                    return BadRequest(new { message = "Mã OTP không hợp lệ hoặc đã hết hạn." });
                }

                return Ok(new { message = "Mã OTP hợp lệ." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during verify-otp");
                return StatusCode(500, new { message = "Lỗi hệ thống khi xác thực OTP" });
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try 
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                if (user == null || user.ResetOtp != request.OtpCode || (user.ResetOtpExpiry != null && user.ResetOtpExpiry < DateTime.UtcNow))
                {
                    return BadRequest(new { message = "Yêu cầu không hợp lệ. Vui lòng xác thực lại OTP." });
                }

                // Update password
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                user.ResetOtp = null; // Clear OTP after use
                user.ResetOtpExpiry = null;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Đặt lại mật khẩu thành công." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during reset-password");
                return StatusCode(500, new { message = "Lỗi hệ thống khi đặt lại mật khẩu" });
            }
        }
    }
}
