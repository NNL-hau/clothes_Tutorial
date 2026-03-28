using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ordering.API.Data;
using Ordering.API.Models;

namespace Ordering.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CouponsController : ControllerBase
    {
        private readonly OrderingDbContext _context;
        private readonly ILogger<CouponsController> _logger;

        public CouponsController(OrderingDbContext context, ILogger<CouponsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/coupons  (Admin - lấy tất cả)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CouponDto>>> GetAll()
        {
            return await _context.Coupons
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => MapToDto(c))
                .ToListAsync();
        }

        // GET: api/coupons/active  (User - lấy mã đang active)
        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<CouponDto>>> GetActive()
        {
            var now = DateTime.UtcNow.AddHours(7);
            return await _context.Coupons
                .Where(c => c.IsActive
                    && (c.ExpiryDate == null || c.ExpiryDate > now)
                    && (c.UsageLimit == null || c.UsedCount < c.UsageLimit))
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => MapToDto(c))
                .ToListAsync();
        }

        // POST: api/coupons/validate  (Kiểm tra và tính toán giảm giá)
        [HttpPost("validate")]
        public async Task<ActionResult<CouponValidateResult>> Validate([FromBody] ValidateCouponRequest req)
        {
            var code = req.Code?.Trim().ToUpper();
            if (string.IsNullOrEmpty(code))
                return BadRequest(new CouponValidateResult { Success = false, Message = "Mã không hợp lệ." });

            var now = DateTime.UtcNow.AddHours(7);
            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Code == code);

            if (coupon == null)
                return Ok(new CouponValidateResult { Success = false, Message = "Mã giảm giá không tồn tại." });

            if (!coupon.IsActive)
                return Ok(new CouponValidateResult { Success = false, Message = "Mã giảm giá đã bị vô hiệu hóa." });

            if (coupon.ExpiryDate.HasValue && coupon.ExpiryDate.Value < now)
                return Ok(new CouponValidateResult { Success = false, Message = "Mã giảm giá đã hết hạn." });

            if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit.Value)
                return Ok(new CouponValidateResult { Success = false, Message = "Mã giảm giá đã hết lượt sử dụng." });

            if (req.OrderAmount < coupon.MinOrderAmount)
                return Ok(new CouponValidateResult
                {
                    Success = false,
                    Message = $"Đơn hàng tối thiểu {coupon.MinOrderAmount:N0}đ để sử dụng mã này."
                });

            decimal discountAmount;
            if (coupon.DiscountType == "Percent")
            {
                discountAmount = req.OrderAmount * coupon.DiscountValue / 100;
                if (coupon.MaxDiscount.HasValue && discountAmount > coupon.MaxDiscount.Value)
                    discountAmount = coupon.MaxDiscount.Value;
            }
            else
            {
                discountAmount = coupon.DiscountValue;
                if (discountAmount > req.OrderAmount)
                    discountAmount = req.OrderAmount;
            }

            return Ok(new CouponValidateResult
            {
                Success = true,
                Message = $"Áp dụng thành công! Giảm {discountAmount:N0}đ",
                DiscountAmount = discountAmount,
                CouponId = coupon.Id,
                Code = coupon.Code,
                Description = coupon.Description
            });
        }

        // POST: api/coupons/use/{id}  (Tăng UsedCount sau khi đặt hàng thành công)
        [HttpPost("use/{id}")]
        public async Task<IActionResult> UseCoupon(Guid id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null) return NotFound();
            coupon.UsedCount++;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // POST: api/coupons  (Admin tạo mã)
        [HttpPost]
        public async Task<ActionResult<CouponDto>> Create([FromBody] CreateCouponDto dto)
        {
            var code = dto.Code.Trim().ToUpper();
            if (await _context.Coupons.AnyAsync(c => c.Code == code))
                return BadRequest("Mã giảm giá này đã tồn tại.");

            var coupon = new Coupon
            {
                Code = code,
                Description = dto.Description,
                DiscountType = dto.DiscountType,
                DiscountValue = dto.DiscountValue,
                MaxDiscount = dto.MaxDiscount,
                MinOrderAmount = dto.MinOrderAmount,
                UsageLimit = dto.UsageLimit,
                IsActive = dto.IsActive,
                ExpiryDate = dto.ExpiryDate
            };

            _context.Coupons.Add(coupon);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetAll), new { id = coupon.Id }, MapToDto(coupon));
        }

        // PUT: api/coupons/{id}  (Admin sửa mã)
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] CreateCouponDto dto)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null) return NotFound();

            var code = dto.Code.Trim().ToUpper();
            if (await _context.Coupons.AnyAsync(c => c.Code == code && c.Id != id))
                return BadRequest("Mã giảm giá này đã tồn tại.");

            coupon.Code = code;
            coupon.Description = dto.Description;
            coupon.DiscountType = dto.DiscountType;
            coupon.DiscountValue = dto.DiscountValue;
            coupon.MaxDiscount = dto.MaxDiscount;
            coupon.MinOrderAmount = dto.MinOrderAmount;
            coupon.UsageLimit = dto.UsageLimit;
            coupon.IsActive = dto.IsActive;
            coupon.ExpiryDate = dto.ExpiryDate;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/coupons/{id}  (Admin xóa mã)
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null) return NotFound();
            _context.Coupons.Remove(coupon);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private static CouponDto MapToDto(Coupon c) => new CouponDto(
            c.Id, c.Code, c.Description, c.DiscountType, c.DiscountValue,
            c.MaxDiscount, c.MinOrderAmount, c.UsageLimit, c.UsedCount,
            c.IsActive, c.ExpiryDate, c.CreatedAt);
    }

    // DTOs
    public record CouponDto(
        Guid Id, string Code, string Description, string DiscountType,
        decimal DiscountValue, decimal? MaxDiscount, decimal MinOrderAmount,
        int? UsageLimit, int UsedCount, bool IsActive,
        DateTime? ExpiryDate, DateTime CreatedAt);

    public record CreateCouponDto(
        string Code, string Description, string DiscountType,
        decimal DiscountValue, decimal? MaxDiscount, decimal MinOrderAmount,
        int? UsageLimit, bool IsActive, DateTime? ExpiryDate);

    public record ValidateCouponRequest(string Code, decimal OrderAmount);

    public record CouponValidateResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal DiscountAmount { get; set; }
        public Guid? CouponId { get; set; }
        public string? Code { get; set; }
        public string? Description { get; set; }
    }
}
