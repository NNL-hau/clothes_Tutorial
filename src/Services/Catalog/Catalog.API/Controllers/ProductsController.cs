using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Catalog.API.Data;
using Catalog.API.Models;
using Catalog.API.DTOs;

namespace Catalog.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly CatalogDbContext _context;

        public ProductsController(CatalogDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts()
        {
            return await _context.Products
                .Select(p => new ProductDto(p.Id, p.Name, p.Description, p.Price, p.ImageUrl, p.CategoryId, p.StockQuantity, p.CreatedAt))
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDto>> GetProduct(Guid id)
        {
            var p = await _context.Products.FindAsync(id);
            if (p == null) return NotFound();

            return new ProductDto(p.Id, p.Name, p.Description, p.Price, p.ImageUrl, p.CategoryId, p.StockQuantity, p.CreatedAt);
        }

        [HttpPost]
        public async Task<ActionResult<ProductDto>> CreateProduct(CreateProductDto dto)
        {
            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                ImageUrl = dto.ImageUrl,
                CategoryId = dto.CategoryId,
                StockQuantity = dto.StockQuantity
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, 
                new ProductDto(product.Id, product.Name, product.Description, product.Price, product.ImageUrl, product.CategoryId, product.StockQuantity, product.CreatedAt));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(Guid id, CreateProductDto dto)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.ImageUrl = dto.ImageUrl;
            product.CategoryId = dto.CategoryId;
            product.StockQuantity = dto.StockQuantity;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(Guid id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
