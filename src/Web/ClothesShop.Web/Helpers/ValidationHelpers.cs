namespace ClothesShop.Web.Helpers
{
    public static class ValidationHelpers
    {
        // Email validation
        public static bool ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            var emailRegex = new System.Text.RegularExpressions.Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            return emailRegex.IsMatch(email);
        }

        // URL validation
        public static bool ValidateUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return true; // Optional field

            return Uri.TryCreate(url, UriKind.Absolute, out var uri) 
                   && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        // Price validation
        public static string? ValidatePrice(decimal price, decimal min = 1000, decimal max = 100000000)
        {
            if (price < min)
                return $"Giá phải từ {min:N0}₫ trở lên";
            if (price > max)
                return $"Giá không được vượt quá {max:N0}₫";
            return null;
        }

        // Stock validation
        public static string? ValidateStock(int stock, int min = 0, int max = 10000)
        {
            if (stock < min)
                return $"Số lượng tồn kho phải từ {min} trở lên";
            if (stock > max)
                return $"Số lượng tồn kho không được vượt quá {max:N0}";
            return null;
        }

        // Product name validation
        public static string? ValidateProductName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Tên sản phẩm không được để trống";
            
            if (name.Length < 3)
                return "Tên sản phẩm phải có ít nhất 3 ký tự";
            
            if (name.Length > 200)
                return "Tên sản phẩm không được quá 200 ký tự";
            
            return null;
        }

        // Banner title validation
        public static string? ValidateBannerTitle(string? title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return "Tiêu đề không được để trống";
            
            if (title.Length < 3)
                return "Tiêu đề phải có ít nhất 3 ký tự";
            
            if (title.Length > 200)
                return "Tiêu đề không được quá 200 ký tự";
            
            return null;
        }

        // Image URL validation
        public static string? ValidateImageUrl(string? url, bool required = false)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return required ? "URL hình ảnh không được để trống" : null;
            }

            if (!ValidateUrl(url))
                return "URL hình ảnh không hợp lệ (phải bắt đầu bằng http:// hoặc https://)";
            
            // Check if URL ends with image extension
            var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg" };
            var uri = new Uri(url);
            var path = uri.AbsolutePath.ToLowerInvariant();
            
            if (!validExtensions.Any(ext => path.EndsWith(ext)))
                return "URL phải trỏ đến file ảnh (.jpg, .png, .gif, .webp, .svg)";
            
            return null;
        }

        // Display order validation
        public static string? ValidateDisplayOrder(int order, int min = 0, int max = 1000)
        {
            if (order < min)
                return $"Thứ tự hiển thị phải từ {min} trở lên";
            if (order > max)
                return $"Thứ tự hiển thị không được vượt quá {max}";
            return null;
        }
    }
}
