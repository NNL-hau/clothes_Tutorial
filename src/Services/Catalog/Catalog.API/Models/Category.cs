using System.ComponentModel.DataAnnotations;

namespace Catalog.API.Models
{
    public class Category
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
