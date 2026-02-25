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

        [HttpGet("product/{productId}")]
        public async Task<ActionResult<IEnumerable<ReviewDto>>> GetReviewsByProductId(Guid productId)
        {
            return await _context.Reviews
                .Where(r => r.ProductId == productId && r.IsApproved)
                .Select(r => new ReviewDto(r.Id, r.ProductId, r.UserName, r.Rating, r.Comment, r.CreatedAt, r.IsApproved))
                .ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<ReviewDto>> CreateReview(CreateReviewDto dto)
        {
            var review = new ProductReview
            {
                ProductId = dto.ProductId,
                UserName = dto.UserName,
                Rating = dto.Rating,
                Comment = dto.Comment,
                IsApproved = true // Auto-approve for demo purposes
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetReviews), new { id = review.Id },
                new ReviewDto(review.Id, review.ProductId, review.UserName, review.Rating, review.Comment, review.CreatedAt, review.IsApproved));
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
