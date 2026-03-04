namespace ClothesShop.Web.Helpers
{
    public static class ProductHelpers
    {
        public static string GetFirstImageUrl(string? imageUrl, string placeholder = "/images/placeholder-product.jpg")
        {
            if (string.IsNullOrEmpty(imageUrl))
                return placeholder;

            var firstImage = imageUrl.Split(',')
                .Select(i => i.Trim())
                .FirstOrDefault(i => !string.IsNullOrEmpty(i));

            return firstImage ?? placeholder;
        }
    }
}
