using System.ComponentModel.DataAnnotations;

namespace Identity.API.Models
{
    public class User
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;
        
        [Required]
        public string Role { get; set; } = "Customer"; // "Customer" or "Admin"
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public string? PhoneNumber { get; set; }
        
        public string? Address { get; set; }

        public string? ResetOtp { get; set; }

        public DateTime? ResetOtpExpiry { get; set; }
    }
}
