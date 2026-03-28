using System.ComponentModel.DataAnnotations;

namespace Catalog.API.Models
{
    public class Banner
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [MaxLength(200)]
        public string? Title { get; set; } = string.Empty;
        
        public string? SubTitle { get; set; }
        
        [Required]
        public string ImageUrl { get; set; } = string.Empty;
        
        public string? LinkUrl { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public int DisplayOrder { get; set; }
    }
}
