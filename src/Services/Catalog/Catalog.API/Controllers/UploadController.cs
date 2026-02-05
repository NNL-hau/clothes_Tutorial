using Microsoft.AspNetCore.Mvc;

namespace Catalog.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<UploadController> _logger;

        public UploadController(IWebHostEnvironment env, ILogger<UploadController> logger)
        {
            _env = env;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<UploadResult>> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "No file uploaded" });

            // Validate file size (5MB max)
            const long maxFileSize = 5 * 1024 * 1024;
            if (file.Length > maxFileSize)
                return BadRequest(new { error = "File size exceeds 5MB limit" });

            // Validate file type
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
            if (!allowedTypes.Contains(file.ContentType))
                return BadRequest(new { error = "Invalid file type. Only JPG, PNG, GIF, WEBP are allowed" });

            try
            {
                // Create uploads directory if not exists
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generate unique filename
                var extension = Path.GetExtension(file.FileName);
                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Return URL
                var url = $"http://localhost:5002/uploads/{fileName}";
                _logger.LogInformation("File uploaded successfully: {FileName}", fileName);

                return Ok(new UploadResult(url));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                return StatusCode(500, new { error = "Upload failed. Please try again." });
            }
        }
    }

    public record UploadResult(string Url);
}
