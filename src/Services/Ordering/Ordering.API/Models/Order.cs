using System.ComponentModel.DataAnnotations;

namespace Ordering.API.Models
{
    public class Order
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public string UserName { get; set; } = string.Empty;
        
        public decimal TotalPrice { get; set; }
        
        // Detailed Customer Info
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? EmailAddress { get; set; }
        public string? Province { get; set; }
        public string? District { get; set; }
        public string? Ward { get; set; }
        public string? AddressDetail { get; set; }

        public string? AddressLine { get; set; } // Legacy field
        public string? Country { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }

        // Payment
        public string? CardName { get; set; }
        public string? CardNumber { get; set; }
        public string? Expiration { get; set; }
        public string? CVV { get; set; }
        public string? PaymentMethodName { get; set; } // e.g., "COD", "MoMo"
        public int PaymentMethod { get; set; }

        // Discount / Coupon
        public string? CouponCode { get; set; }
        public decimal CouponAmount { get; set; }

        [Required]
        public string OrderStatus { get; set; } = "Pending"; // Pending, InProgress, Shipped, Cancelled

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(7);

        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
