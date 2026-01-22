using System.ComponentModel.DataAnnotations;

namespace Review.API.Models
{
    public class ProductReview
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public Guid ProductId { get; set; }
        
        [Required]
        public string UserName { get; set; } = string.Empty;
        
        [Range(1, 5)]
        public int Rating { get; set; }
        
        public string? Comment { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public bool IsApproved { get; set; } = true;
    }
}
