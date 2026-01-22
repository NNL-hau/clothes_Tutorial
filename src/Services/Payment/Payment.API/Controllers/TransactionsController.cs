using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Payment.API.Data;
using Payment.API.Models;
using Payment.API.DTOs;

namespace Payment.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionsController : ControllerBase
    {
        private readonly PaymentDbContext _context;

        public TransactionsController(PaymentDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TransactionDto>>> GetTransactions()
        {
            return await _context.Transactions
                .Select(t => new TransactionDto(t.Id, t.OrderId, t.UserName, t.Amount, t.PaymentMethod, t.Status, t.CreatedAt))
                .ToListAsync();
        }
    }
}
