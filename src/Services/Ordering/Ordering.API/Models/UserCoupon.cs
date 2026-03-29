using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ordering.API.Models
{
    public class UserCoupon
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required]
        public Guid CouponId { get; set; }

        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow.AddHours(7);
        
        public bool IsUsed { get; set; } = false;
        
        public DateTime? UsedAt { get; set; }

        // Navigation property
        [ForeignKey("CouponId")]
        public virtual Coupon? Coupon { get; set; }
    }
}
