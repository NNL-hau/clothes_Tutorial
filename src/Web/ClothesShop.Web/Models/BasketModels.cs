namespace ClothesShop.Web.Models
{
    public class BasketItem
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public int Quantity { get; set; }
        public int StockQuantity { get; set; }
        public string? SelectedColor { get; set; }
        public string? SelectedSize { get; set; }
        public string? AvailableColors { get; set; }
        public string? AvailableSizes { get; set; }
        public bool IsSelected { get; set; } = true;
    }

    public class CustomerBasket
    {
        public List<BasketItem> Items { get; set; } = new();
        public decimal TotalPrice => Items.Where(i => i.IsSelected).Sum(i => i.Price * i.Quantity);
        public int TotalItems => Items.Where(i => i.IsSelected).Sum(i => i.Quantity);
    }
}
