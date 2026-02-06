using ClothesShop.Web.Models;

namespace ClothesShop.Web.Services
{
    public interface IBasketService
    {
        event Action OnChange;
        Task<CustomerBasket> GetBasketAsync();
        Task AddToBasketAsync(ProductDto product);
        Task RemoveFromBasketAsync(Guid productId);
        Task ClearBasketAsync();
        Task<int> GetBasketItemCountAsync();
        Task UpdateQuantityAsync(Guid productId, int quantity);
    }
}
