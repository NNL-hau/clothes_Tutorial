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

        [HttpGet("suggestions")]
        public async Task<ActionResult<IEnumerable<SuggestionDto>>> GetSuggestions()
        {
            var suggestions = new List<SuggestionDto>();

            try
            {
                // 1. Fetch top 2 best selling products
                var topProducts = await _context.Products
                    .OrderByDescending(p => p.SoldQuantity)
                    .Take(2)
                    .ToListAsync();

                foreach (var p in topProducts)
                {
                    suggestions.Add(new SuggestionDto($"Cho tôi xem {p.Name}", "bx-star"));
                }

                // 2. Fetch 2 random categories
                var categories = await _context.Categories
                    .OrderBy(c => Guid.NewGuid())
                    .Take(2)
                    .ToListAsync();

                foreach (var c in categories)
                {
                    suggestions.Add(new SuggestionDto($"Sản phẩm thuộc mục {c.Name}", "bx-category"));
                }

                // 3. Fallback if empty
                if (!suggestions.Any())
                {
                    suggestions.Add(new SuggestionDto("Xu hướng thời trang mới nhất", "bx-trending-up"));
                    suggestions.Add(new SuggestionDto("Các sản phẩm đang giảm giá", "bx-purchase-tag-alt"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching chat suggestions");
                // Return default suggestions on error
                return Ok(new List<SuggestionDto>
                {
                    new("Cho tôi xem áo sơ mi", "bx-shirt"),
                    new("Hàng mới về", "bx-news")
                });
            }

            return Ok(suggestions);
        }

        [HttpPost]
        public async Task<ActionResult<ChatResponse>> PostChat([FromBody] ChatRequest request)
        {
            if (string.IsNullOrEmpty(request.Message))
            {
                return BadRequest("Message cannot be empty.");
            }

            // 1. Always load full catalog from DB so AI has complete context to reason from
            List<Catalog.API.Models.Category> categories = new();
            List<Catalog.API.Models.Product> allProducts = new();

            try 
            {
                categories = await _context.Categories.ToListAsync();
                
                // Load ALL products (with a reasonable cap of 100) so Gemini can reason
                // about the full inventory without being limited by keyword matching.
                allProducts = await _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.StockQuantity > 0) // only in-stock products
                    .OrderByDescending(p => p.SoldQuantity) // put best-sellers first
                    .Take(100)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
            }

            // 2. Build rich context for Gemini
            var contextBuilder = new StringBuilder();
            contextBuilder.AppendLine("Bạn là chuyên gia tư vấn thời trang của cửa hàng ClothesShop. Bạn có quyền truy cập toàn bộ dữ liệu kho hàng thực tế. Hãy luôn tư vấn dựa trên dữ liệu thực bên dưới.");
            
            if (!string.IsNullOrEmpty(request.Username))
                contextBuilder.AppendLine($"Khách hàng tên là: {request.Username}. Hãy xưng hô thân thiện.");

            // --- DANH MỤC ---
            contextBuilder.AppendLine($"\n=== DANH MỤC SẢN PHẨM ({categories.Count} danh mục) ===");
            contextBuilder.AppendLine(string.Join(", ", categories.Select(c => c.Name)));

            // --- TOÀN BỘ SẢN PHẨM TRONG KHO ---
            contextBuilder.AppendLine($"\n=== DANH SÁCH SẢN PHẨM TRONG KHO ({allProducts.Count} sản phẩm) ===");
            if (allProducts.Any())
            {
                foreach (var product in allProducts)
                {
                    contextBuilder.AppendLine(
                        $"• [{product.Category.Name}] {product.Name} | Giá: {product.Price:N0}đ | Tồn: {product.StockQuantity} | Đã bán: {product.SoldQuantity} | Màu: {product.Colors} | Size: {product.Sizes} | Mô tả: {product.Description}");
                }
            }
            else
            {
                contextBuilder.AppendLine("(Hiện chưa có sản phẩm trong kho)");
            }

            // --- THÔNG TIN CÁ NHÂN KHÁCH HÀNG ---
            contextBuilder.AppendLine("\n=== THÔNG TIN CÁ NHÂN KHÁCH HÀNG ===");
            if (!string.IsNullOrEmpty(request.UserProfile))
                contextBuilder.AppendLine($"Hồ sơ: {request.UserProfile}");
            if (!string.IsNullOrEmpty(request.BasketContext))
                contextBuilder.AppendLine($"Giỏ hàng hiện tại: {request.BasketContext}");
            if (!string.IsNullOrEmpty(request.OrderContext))
                contextBuilder.AppendLine($"Lịch sử đơn hàng gần đây: {request.OrderContext}");

            // --- QUY TẮC TRẢ LỜI ---
            contextBuilder.AppendLine(@"
=== QUY TẮC BẮT BUỘC ===
1. Luôn dựa vào danh sách sản phẩm thực tế ở trên để đưa ra gợi ý cụ thể (tên, giá, màu sắc, size).
2. Khi khách hỏi 'gợi ý', 'tôi nên mua gì', hãy chọn 3-5 sản phẩm phù hợp nhất từ danh sách và trả lời cụ thể.
3. Khi khách hỏi về giỏ hàng/đơn hàng/thông tin cá nhân, dùng phần THÔNG TIN CÁ NHÂN.
4. TUYỆT ĐỐI không được bịa tên sản phẩm, giá hay thông tin không có trong dữ liệu trên.
5. Trả lời bằng TIẾNG VIỆT, thân thiện, chuyên nghiệp và kèm chi tiết giá cả.");


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
