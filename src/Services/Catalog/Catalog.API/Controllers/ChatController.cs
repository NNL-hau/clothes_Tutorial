using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Catalog.API.Data;
using Catalog.API.DTOs;
using System.Text;
using System.Text.Json;

namespace Catalog.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly CatalogDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ChatController> _logger;

        public ChatController(
            CatalogDbContext context,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ILogger<ChatController> logger)
        {
            _context = context;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<ChatResponse>> PostChat([FromBody] ChatRequest request)
        {
            if (string.IsNullOrEmpty(request.Message))
            {
                return BadRequest("Message cannot be empty.");
            }

            // 1. Fetch comprehensive data for the AI context
            List<Catalog.API.Models.Category> categories = new();
            List<Catalog.API.Models.Product> relevantProducts = new();

            try 
            {
                // Fetch all categories to give the AI an idea of the shop structure
                categories = await _context.Categories.ToListAsync();
                
                // Fetch products matching the search OR just fetch a slice of featured products if search is broad
                var searchTerm = request.Message.ToLower();
                relevantProducts = await _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.Name.ToLower().Contains(searchTerm) || 
                                (p.Description != null && p.Description.ToLower().Contains(searchTerm)) ||
                                p.Category.Name.ToLower().Contains(searchTerm))
                    .Take(20) // Increased from 5 to 20 for better "read everything" feel
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                // Log the error (simplified for now)
                Console.WriteLine($"Database error: {ex.Message}");
                // We proceed with empty lists if DB fails, so AI can at least respond generally
            }

            // 2. Build the context for Gemini
            var contextBuilder = new StringBuilder();
            contextBuilder.AppendLine("Bạn là chuyên gia tư vấn thời trang am hiểu cho cửa hàng ClothesShop.");
            if (!string.IsNullOrEmpty(request.Username))
            {
                contextBuilder.AppendLine($"Khách hàng tên là: {request.Username}. Hãy chào và xưng hô thân thiện.");
            }
            contextBuilder.AppendLine("Các danh mục sản phẩm: " + string.Join(", ", categories.Select(c => c.Name)));
            
            if (relevantProducts.Any())
            {
                contextBuilder.AppendLine("\nCác sản phẩm tiêu biểu/liên quan trong cửa hàng:");
                foreach (var product in relevantProducts)
                {
                    contextBuilder.AppendLine($"- {product.Name} ({product.Category.Name}): {product.Price:C}. {product.Description}");
                }
            }
            else
            {
                contextBuilder.AppendLine("\nTôi không tìm thấy sản phẩm cụ thể cho yêu cầu này, nhưng chúng tôi có nhiều mặt hàng thời trang đa dạng trong các danh mục trên.");
            }
            contextBuilder.AppendLine("\nHướng dẫn: Sử dụng dữ liệu trên để trả lời một cách chi tiết, chuyên nghiệp và thân thiện bằng TIẾNG VIỆT. Nếu có nhiều sản phẩm phù hợp, hãy gợi ý một vài mẫu. Nếu nhắc đến giá, hãy sử dụng mức giá chính xác đã cung cấp. Nếu không thấy sản phẩm, hãy gợi ý khách hàng xem các danh mục sản phẩm của chúng tôi.");

            // 3. Call Gemini API
            // NOTE: API key is read from configuration for security.
            // Make sure you have GeminiSettings:ApiKey configured via appsettings or environment variables.
            var environment = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Unknown";
            var apiKey = _configuration["GeminiSettings:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogError(
                    "Gemini API key missing. Environment={Environment}. " +
                    "Check appsettings.Development.json or environment variables for GeminiSettings:ApiKey.",
                    environment);

                return StatusCode(500, "Gemini API key is not configured. Please set GeminiSettings:ApiKey.");
            }

            // Use the Gemini 2.5 Flash model (your quota screenshot shows limits for this model)
            var modelName = "gemini-2.5-flash";
            var apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={apiKey}";

            var geminiRequest = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[]
                        {
                            new { text = $"{contextBuilder}\nCâu hỏi của khách hàng: {request.Message}" }
                        }
                    }
                }
            };

            using var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsJsonAsync(apiUrl, geminiRequest);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, $"Error calling AI service: {errorContent}");
            }

            var geminiResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
            var aiText = geminiResponse.GetProperty("candidates")[0]
                                      .GetProperty("content")
                                      .GetProperty("parts")[0]
                                      .GetProperty("text")
                                      .GetString();

            return Ok(new ChatResponse(aiText ?? "I'm sorry, I couldn't generate a response."));
        }
    }
}
