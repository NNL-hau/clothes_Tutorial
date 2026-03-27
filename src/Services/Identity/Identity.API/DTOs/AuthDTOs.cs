using System.ComponentModel.DataAnnotations;

namespace Identity.API.DTOs
{
    public class RegisterRequest
    {
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        public string Password { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        public string FullName { get; set; } = string.Empty;
        
        public string? PhoneNumber { get; set; }
        
        public string? Address { get; set; }
    }
    
    public class LoginRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập Tên đăng nhập")]
        public string UserIdentifier { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        public string Password { get; set; } = string.Empty;
    }

    public class GoogleLoginRequest
    {
        [Required]
        public string Token { get; set; } = string.Empty;
    }
    
    public class AuthResponse
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }
    
    public class UserResponse
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UpdateProfileRequest
    {
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? NewPassword { get; set; }
    }

    public class ForgotPasswordRequest
    {
        [Required]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class VerifyOtpRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string OtpCode { get; set; } = string.Empty;
    }

    public class ResetPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string OtpCode { get; set; } = string.Empty;
        
        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
