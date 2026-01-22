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

        public OrdersController(OrderingDbContext context)
        {
            _context = context;
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
                    o.OrderItems.Select(oi => new OrderItemDto(oi.Id, oi.ProductId, oi.ProductName, oi.Price, oi.Quantity)).ToList()))
                .ToListAsync();
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
                o.OrderItems.Select(oi => new OrderItemDto(oi.Id, oi.ProductId, oi.ProductName, oi.Price, oi.Quantity)).ToList());
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusDto dto)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            order.OrderStatus = dto.Status;
            await _context.SaveChangesAsync();
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
