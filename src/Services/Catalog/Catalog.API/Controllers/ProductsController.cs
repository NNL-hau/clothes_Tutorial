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
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(CatalogDbContext context, ILogger<ProductsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts()
        {
            return await _context.Products
                .Select(p => new ProductDto(p.Id, p.Name, p.Description, p.Price, p.ImageUrl, p.CategoryId, p.StockQuantity, p.SoldQuantity, p.Colors, p.Sizes, p.CreatedAt))
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDto>> GetProduct(Guid id)
        {
            var p = await _context.Products.FindAsync(id);
            if (p == null) return NotFound();

            return new ProductDto(p.Id, p.Name, p.Description, p.Price, p.ImageUrl, p.CategoryId, p.StockQuantity, p.SoldQuantity, p.Colors, p.Sizes, p.CreatedAt);
        }

        [HttpGet("{id}/related")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetRelatedProducts(Guid id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            return await _context.Products
                .Where(p => p.CategoryId == product.CategoryId && p.Id != id)
                .Take(4)
                .Select(p => new ProductDto(p.Id, p.Name, p.Description, p.Price, p.ImageUrl, p.CategoryId, p.StockQuantity, p.SoldQuantity, p.Colors, p.Sizes, p.CreatedAt))
                .ToListAsync();
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
                StockQuantity = dto.StockQuantity,
                Colors = dto.Colors,
                Sizes = dto.Sizes
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, 
                new ProductDto(product.Id, product.Name, product.Description, product.Price, product.ImageUrl, product.CategoryId, product.StockQuantity, product.SoldQuantity, product.Colors, product.Sizes, product.CreatedAt));
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
            product.Colors = dto.Colors;
            product.Sizes = dto.Sizes;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPatch("{id}/deduct-stock")]
        public async Task<IActionResult> DeductStock(Guid id, [FromQuery] int quantity)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            if (product.StockQuantity < quantity)
            {
                return BadRequest("Not enough stock available.");
            }

            Console.WriteLine($"[Catalog.API] Deducting stock for {id}. Old Stock: {product.StockQuantity}, Old Sold: {product.SoldQuantity}, Qty: {quantity}");
            product.StockQuantity -= quantity;
            product.SoldQuantity += quantity;
            await _context.SaveChangesAsync();
            Console.WriteLine($"[Catalog.API] Deducted stock for {id}. New Stock: {product.StockQuantity}, New Sold: {product.SoldQuantity}");
            return NoContent();

        }

        [HttpPatch("{id}/restore-stock")]
        public async Task<IActionResult> RestoreStock(Guid id, [FromQuery] int quantity)
        {
            _logger.LogInformation("[Catalog API] Received restore-stock request for Product: {ProductGuid}, Quantity: {Qty}", id, quantity);
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                _logger.LogWarning("[Catalog API] Product {ProductGuid} not found for restore-stock", id);
                return NotFound();
            }

            _logger.LogInformation("[Catalog API] Restoring stock for {ProductName}. Old Stock: {OldStock}, Old Sold: {OldSold}, Restore Qty: {Qty}", 
                product.Name, product.StockQuantity, product.SoldQuantity, quantity);
            
            product.StockQuantity += quantity;
            product.SoldQuantity = Math.Max(0, product.SoldQuantity - quantity);
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("[Catalog API] Successfully restored stock for {ProductName}. New Stock: {NewStock}, New Sold: {NewSold}", 
                product.Name, product.StockQuantity, product.SoldQuantity);
            
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
