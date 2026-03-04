using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Catalog.API.Models
{
    public class Product
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        
        public string? ImageUrl { get; set; }
        
        public Guid CategoryId { get; set; }
        public virtual Category Category { get; set; } = null!;
        
        public int StockQuantity { get; set; }
        public int SoldQuantity { get; set; }

        
        public string? Colors { get; set; } // e.g., "Đen, Trắng, Xanh"
        public string? Sizes { get; set; }  // e.g., "S, M, L, XL"
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
