using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Review.API.Data;
using Review.API.Models;
using Review.API.DTOs;

namespace Review.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewsController : ControllerBase
    {
        private readonly ReviewDbContext _context;

        public ReviewsController(ReviewDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReviewDto>>> GetReviews()
        {
            return await _context.Reviews
                .Select(r => new ReviewDto(r.Id, r.ProductId, r.UserName, r.Rating, r.Comment, r.CreatedAt, r.IsApproved))
                .ToListAsync();
        }

        [HttpPatch("{id}/approve")]
        public async Task<IActionResult> ApproveReview(Guid id, [FromBody] bool approve)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return NotFound();

            review.IsApproved = approve;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReview(Guid id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return NotFound();

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
