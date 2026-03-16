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
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders()
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
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
                    o.OrderItems.Select(oi => new OrderItemDto(oi.Id, oi.ProductId, oi.ProductName, oi.Price, oi.Quantity)).ToList()))
                .ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderDto dto)
        {
            _logger.LogInformation("Creating order for User: {UserName}, FullName: {FullName}", dto.UserName, dto.FullName);
            
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
                OrderStatus = "AwaitingPayment",
                OrderItems = dto.OrderItems.Select(oi => new OrderItem
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.ProductName,
                    Price = oi.Price,
                    Quantity = oi.Quantity
                }).ToList()
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

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
            order.OrderStatus = dto.Status;
            await _context.SaveChangesAsync();

            // Nếu chuyển trạng thái sang Pending (Thanh toán thành công)
            // thì thực hiện trừ tồn kho tại Catalog API
            if (dto.Status == "Pending" && oldStatus != "Pending")
            {
                _logger.LogInformation("Order {OrderId} paid successfully. Deducting stock...", id);
                
                var catalogUrl = _configuration["CatalogApiUrl"];
                if (string.IsNullOrEmpty(catalogUrl))
                {
                    _logger.LogError("CatalogApiUrl is not configured!");
                }
                else 
                {
                    using var client = new HttpClient();
                    foreach (var item in order.OrderItems)
                    {
                        try 
                        {
                            // PATCH /api/products/{id}/deduct-stock?quantity={qty}
                            var response = await client.PatchAsync($"{catalogUrl}/{item.ProductId}/deduct-stock?quantity={item.Quantity}", null);
                            
                            if (response.IsSuccessStatusCode)
                            {
                                _logger.LogInformation("Successfully deducted {Quantity} for Product {ProductId}", item.Quantity, item.ProductId);
                            }
                            else 
                            {
                                var error = await response.Content.ReadAsStringAsync();
                                _logger.LogError("Failed to deduct stock for Product {ProductId}: {Error}", item.ProductId, error);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error calling Catalog API for Product {ProductId}", item.ProductId);
                        }
                    }
                }
            }

            return NoContent();
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
