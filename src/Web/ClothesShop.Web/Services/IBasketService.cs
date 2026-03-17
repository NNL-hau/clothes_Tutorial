using ClothesShop.Web.Models;

namespace ClothesShop.Web.Services
{
    public interface IBasketService
    {
        event Action OnChange;
        Task<CustomerBasket> GetBasketAsync();
        Task AddToBasketAsync(ProductDto product, string? selectedColor = null, string? selectedSize = null);
        Task RemoveFromBasketAsync(Guid productId, string? color = null, string? size = null);
        Task ClearBasketAsync();
        Task RemoveSelectedItemsAsync();
        Task<int> GetBasketItemCountAsync();
        Task UpdateQuantityAsync(Guid productId, string? color, string? size, int quantity);
        Task UpdateOptionsAsync(Guid productId, string? oldColor, string? oldSize, string? newColor, string? newSize);
        Task ToggleSelectionAsync(Guid productId, string? color, string? size, bool isSelected);
        Task ToggleAllSelectionAsync(bool isSelected);
    }
}
