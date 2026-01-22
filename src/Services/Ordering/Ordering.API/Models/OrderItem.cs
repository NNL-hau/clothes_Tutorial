using System.ComponentModel.DataAnnotations;

namespace Ordering.API.Models
{
    public class OrderItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public Guid ProductId { get; set; }
        
        [Required]
        public string ProductName { get; set; } = string.Empty;
        
        public decimal Price { get; set; }
        
        public int Quantity { get; set; }
        
        public Guid OrderId { get; set; }
        public virtual Order Order { get; set; } = null!;
    }
}
