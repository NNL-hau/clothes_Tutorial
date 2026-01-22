using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Catalog.API.Data;
using Catalog.API.Models;
using Catalog.API.DTOs;

namespace Catalog.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BannersController : ControllerBase
    {
        private readonly CatalogDbContext _context;

        public BannersController(CatalogDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BannerDto>>> GetBanners()
        {
            return await _context.Banners
                .Select(b => new BannerDto(b.Id, b.Title, b.SubTitle, b.ImageUrl, b.LinkUrl, b.IsActive, b.DisplayOrder))
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BannerDto>> GetBanner(Guid id)
        {
            var b = await _context.Banners.FindAsync(id);
            if (b == null) return NotFound();
            return new BannerDto(b.Id, b.Title, b.SubTitle, b.ImageUrl, b.LinkUrl, b.IsActive, b.DisplayOrder);
        }

        [HttpPost]
        public async Task<ActionResult<BannerDto>> CreateBanner(CreateBannerDto dto)
        {
            var banner = new Banner
            {
                Title = dto.Title,
                SubTitle = dto.SubTitle,
                ImageUrl = dto.ImageUrl,
                LinkUrl = dto.LinkUrl,
                IsActive = dto.IsActive,
                DisplayOrder = dto.DisplayOrder
            };

            _context.Banners.Add(banner);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBanner), new { id = banner.Id }, 
                new BannerDto(banner.Id, banner.Title, banner.SubTitle, banner.ImageUrl, banner.LinkUrl, banner.IsActive, banner.DisplayOrder));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBanner(Guid id, CreateBannerDto dto)
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner == null) return NotFound();

            banner.Title = dto.Title;
            banner.SubTitle = dto.SubTitle;
            banner.ImageUrl = dto.ImageUrl;
            banner.LinkUrl = dto.LinkUrl;
            banner.IsActive = dto.IsActive;
            banner.DisplayOrder = dto.DisplayOrder;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBanner(Guid id)
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner == null) return NotFound();

            _context.Banners.Remove(banner);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
