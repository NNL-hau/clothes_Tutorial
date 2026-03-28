using System.ComponentModel.DataAnnotations;

namespace Payment.API.Models
{
    public class Transaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public Guid OrderId { get; set; }
        
        [Required]
        public string UserName { get; set; } = string.Empty;
        
        public decimal Amount { get; set; }
        
        [Required]
        public string PaymentMethod { get; set; } = string.Empty;
        
        [Required]
        public string Status { get; set; } = "Pending"; // Pending, Success, Failed
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(7);
    }
}
