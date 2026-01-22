using System.ComponentModel.DataAnnotations;

namespace Ordering.API.Models
{
    public class Order
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public string UserName { get; set; } = string.Empty;
        
        public decimal TotalPrice { get; set; }
        
        // Billing Address
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? EmailAddress { get; set; }
        public string? AddressLine { get; set; }
        public string? Country { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }

        // Payment
        public string? CardName { get; set; }
        public string? CardNumber { get; set; }
        public string? Expiration { get; set; }
        public string? CVV { get; set; }
        public int PaymentMethod { get; set; }

        [Required]
        public string OrderStatus { get; set; } = "Pending"; // Pending, InProgress, Shipped, Cancelled

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
