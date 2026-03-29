using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ordering.API.Data;
using Ordering.API.Models;
using Ordering.API.DTOs;

namespace Ordering.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly OrderingDbContext _context;
        private readonly ILogger<OrdersController> _logger;
        private readonly IConfiguration _configuration;

        public OrdersController(OrderingDbContext context, ILogger<OrdersController> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders([FromQuery] string? userName)
        {
            var query = _context.Orders.AsQueryable();

            if (!string.IsNullOrEmpty(userName))
            {
                query = query.Where(o => o.UserName == userName);
            }

            return await query
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new OrderDto(
                    o.Id, 
                    o.UserName, 
                    o.TotalPrice, 
                    o.OrderStatus, 
                    o.CreatedAt,
                    o.FullName,
                    o.PhoneNumber,
                    o.Province,
                    o.District,
                    o.Ward,
                    o.AddressDetail,
                    o.PaymentMethodName,
                    o.CouponCode,
                    o.CouponAmount,
                    o.OrderItems.Select(oi => new OrderItemDto(oi.Id, oi.ProductId, oi.ProductName, oi.Price, oi.Quantity)).ToList()))
                .ToListAsync();
        }

        [HttpGet("user/{userName}")]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrdersByUser(string userName)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.UserName == userName)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new OrderDto(
                    o.Id, 
                    o.UserName, 
                    o.TotalPrice, 
                    o.OrderStatus, 
                    o.CreatedAt,
                    o.FullName,
                    o.PhoneNumber,
                    o.Province,
                    o.District,
                    o.Ward,
                    o.AddressDetail,
                    o.PaymentMethodName,
                    o.CouponCode,
                    o.CouponAmount,
                    o.OrderItems.Select(oi => new OrderItemDto(oi.Id, oi.ProductId, oi.ProductName, oi.Price, oi.Quantity)).ToList()))
                .ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderDto dto)
        {
            _logger.LogInformation("Creating order for User: {UserName}, FullName: {FullName}", dto.UserName, dto.FullName);
            
            var isOnlinePayment = dto.PaymentMethodName == "MoMo" || dto.PaymentMethodName == "VNPay";
            var initialStatus = isOnlinePayment ? "AwaitingPayment" : "Pending";

            var order = new Order
            {
                UserName = dto.UserName,
                TotalPrice = dto.TotalPrice,
                FullName = dto.FullName,
                PhoneNumber = dto.PhoneNumber,
                EmailAddress = dto.EmailAddress,
                Province = dto.Province,
                District = dto.District,
                Ward = dto.Ward,
                AddressDetail = dto.AddressDetail,
                AddressLine = dto.AddressLine,
                Country = dto.Country,
                State = dto.State,
                ZipCode = dto.ZipCode,
                CardName = dto.CardName,
                CardNumber = dto.CardNumber,
                Expiration = dto.Expiration,
                CVV = dto.CVV,
                PaymentMethodName = dto.PaymentMethodName,
                PaymentMethod = dto.PaymentMethod,
                CouponCode = dto.CouponCode,
                CouponAmount = dto.CouponAmount,
                OrderStatus = initialStatus,
                OrderItems = dto.OrderItems.Select(oi => new OrderItem
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.ProductName,
                    Price = oi.Price,
                    Quantity = oi.Quantity
                }).ToList()
            };

            _context.Orders.Add(order);

            // Update Coupon usage if a code was applied
            if (!string.IsNullOrEmpty(dto.CouponCode))
            {
                var couponCode = dto.CouponCode.Trim().ToUpper();
                var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == couponCode);
                if (coupon != null)
                {
                    coupon.UsedCount++;
                    
                    // Đánh dấu mã đã dùng cho user cụ thể
                    var userCoupon = await _context.UserCoupons
                        .FirstOrDefaultAsync(uc => uc.UserName == dto.UserName && uc.CouponId == coupon.Id);
                    
                    if (userCoupon == null)
                    {
                        // Nếu user chưa "nhận" từ popup nhưng tự gõ mã, tạo record mới
                        userCoupon = new UserCoupon
                        {
                            UserName = dto.UserName,
                            CouponId = coupon.Id,
                            ReceivedAt = DateTime.UtcNow.AddHours(7)
                        };
                        _context.UserCoupons.Add(userCoupon);
                    }
                    
                    userCoupon.IsUsed = true;
                    userCoupon.UsedAt = DateTime.UtcNow.AddHours(7);
                }
            }

            await _context.SaveChangesAsync();

            // Nếu đơn hàng nhẩy thẳng vào trạng thái Pending (MoMo/VNPay), thực hiện trừ kho luôn
            if (initialStatus == "Pending")
            {
                await DeductStockAsync(order);
            }

            var result = new OrderDto(
                order.Id,
                order.UserName,
                order.TotalPrice,
                order.OrderStatus,
                order.CreatedAt,
                order.FullName,
                order.PhoneNumber,
                order.Province,
                order.District,
                order.Ward,
                order.AddressDetail,
                order.PaymentMethodName,
                order.CouponCode,
                order.CouponAmount,
                order.OrderItems.Select(oi => new OrderItemDto(oi.Id, oi.ProductId, oi.ProductName, oi.Price, oi.Quantity)).ToList());

            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<OrderDto>> GetOrder(Guid id)
        {
            var o = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (o == null) return NotFound();

            return new OrderDto(
                o.Id, 
                o.UserName, 
                o.TotalPrice, 
                o.OrderStatus, 
                o.CreatedAt,
                o.FullName,
                o.PhoneNumber,
                o.Province,
                o.District,
                o.Ward,
                o.AddressDetail,
                o.PaymentMethodName,
                o.CouponCode,
                o.CouponAmount,
                o.OrderItems.Select(oi => new OrderItemDto(oi.Id, oi.ProductId, oi.ProductName, oi.Price, oi.Quantity)).ToList());
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusDto dto)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id);
                
            if (order == null) return NotFound();

            var oldStatus = order.OrderStatus;

            // Simple validation: Cannot cancel if already InProgress or Shipped
            if (dto.Status == "Cancelled" && (oldStatus == "InProgress" || oldStatus == "Shipped"))
            {
                return BadRequest("Không thể hủy đơn hàng đã được xác nhận hoặc đang giao.");
            }

            order.OrderStatus = dto.Status;
            await _context.SaveChangesAsync();

            // Nếu chuyển trạng thái sang Pending (Thanh toán thành công)
            // thì thực hiện trừ tồn kho tại Catalog API
            if (dto.Status == "Pending" && oldStatus != "Pending")
            {
                await DeductStockAsync(order);
            }

            // Nếu hủy đơn mà trạng thái cũ là Pending (kho đã bị trừ),
            // thì hoàn trả lại tồn kho
            if (dto.Status == "Cancelled" && oldStatus == "Pending")
            {
                await RestoreStockAsync(order);
            }

            return NoContent();
        }

        private async Task DeductStockAsync(Order order)
        {
            _logger.LogInformation("Order {OrderId} is in Pending status. Deducting stock...", order.Id);

            var catalogUrl = _configuration["CatalogApiUrl"];
            if (string.IsNullOrEmpty(catalogUrl))
            {
                _logger.LogError("CatalogApiUrl is not configured!");
                return;
            }

            using var client = new HttpClient();
            foreach (var item in order.OrderItems)
            {
                try
                {
                    var url = $"{catalogUrl}/{item.ProductId}/deduct-stock?quantity={item.Quantity}";
                    _logger.LogInformation("[Ordering API] Calling Catalog API: {Url}", url);
                    
                    var response = await client.PatchAsync(url, null);

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("[Ordering API] Successfully deducted stock for Product {ProductId}", item.ProductId);
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        _logger.LogError("[Ordering API] Failed to deduct stock for Product {ProductId}. Status: {Status}, Error: {Error}", item.ProductId, response.StatusCode, error);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Ordering API] Exception calling Catalog API for Product {ProductId}", item.ProductId);
                }
            }
        }

        private async Task RestoreStockAsync(Order order)
        {
            _logger.LogInformation("Order {OrderId} is cancelled from Pending. Restoring stock...", order.Id);

            var catalogUrl = _configuration["CatalogApiUrl"];
            if (string.IsNullOrEmpty(catalogUrl))
            {
                _logger.LogError("CatalogApiUrl is not configured!");
                return;
            }

            using var client = new HttpClient();
            foreach (var item in order.OrderItems)
            {
                try
                {
                    var url = $"{catalogUrl}/{item.ProductId}/restore-stock?quantity={item.Quantity}";
                    _logger.LogInformation("[Ordering API] Calling Catalog API (Restore): {Url}", url);

                    var response = await client.PatchAsync(url, null);

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("[Ordering API] Successfully restored stock for Product {ProductId}", item.ProductId);
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        _logger.LogError("[Ordering API] Failed to restore stock for Product {ProductId}. Status: {Status}, Error: {Error}", item.ProductId, response.StatusCode, error);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Ordering API] Exception calling Catalog API (Restore) for Product {ProductId}", item.ProductId);
                }
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(Guid id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
