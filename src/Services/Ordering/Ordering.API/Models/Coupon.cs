using System.ComponentModel.DataAnnotations;

namespace Ordering.API.Models
{
    public class Coupon
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Code { get; set; } = string.Empty;  // e.g. "SALE10"

        public string Description { get; set; } = string.Empty;

        // DiscountType: "Percent" hoặc "Fixed"
        public string DiscountType { get; set; } = "Percent";

        // Giá trị giảm: 10 => giảm 10%, hoặc 50000 => giảm 50,000đ
        public decimal DiscountValue { get; set; }

        // Giảm tối đa (chỉ áp dụng cho Percent)
        public decimal? MaxDiscount { get; set; }

        // Đơn hàng tối thiểu để áp dụng
        public decimal MinOrderAmount { get; set; } = 0;

        // Số lần dùng tối đa (null = không giới hạn)
        public int? UsageLimit { get; set; }

        // Số lần đã dùng
        public int UsedCount { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime? ExpiryDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(7);
    }
}
