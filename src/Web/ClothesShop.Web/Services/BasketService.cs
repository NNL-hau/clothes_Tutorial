using Blazored.LocalStorage;
using ClothesShop.Web.Models;

namespace ClothesShop.Web.Services
{
    public class BasketService : IBasketService
    {
        private readonly ILocalStorageService _localStorage;
        private readonly IAuthService _authService;
        private const string DefaultBasketKey = "customer_basket";
        public event Action? OnChange;

        public BasketService(ILocalStorageService localStorage, IAuthService authService)
        {
            _localStorage = localStorage;
            _authService = authService;
            
            // Re-notify basket changes when auth state changes
            _authService.OnAuthStateChanged += NotifyStateChanged;
        }

        private string GetBasketKey()
        {
            var user = _authService.CurrentUser;
            if (user != null && !string.IsNullOrEmpty(user.Email))
            {
                return $"basket_{user.Email}";
            }
            return DefaultBasketKey;
        }

        public async Task<CustomerBasket> GetBasketAsync()
        {
            if (!await _authService.IsAuthenticatedAsync())
                return new CustomerBasket();

            var key = GetBasketKey();
            var basket = await _localStorage.GetItemAsync<CustomerBasket>(key);
            return basket ?? new CustomerBasket();
        }

        public async Task AddToBasketAsync(ProductDto product)
        {
            if (!await _authService.IsAuthenticatedAsync())
                return;

            var basket = await GetBasketAsync();
            
            var colors = product.Colors?.Split(',').Select(c => c.Trim()).ToList();
            var sizes = product.Sizes?.Split(',').Select(s => s.Trim()).ToList();
            
            var defaultColor = colors?.FirstOrDefault();
            var defaultSize = sizes?.FirstOrDefault();

            // Try to find item with same product ID AND default selected options
            var item = basket.Items.FirstOrDefault(i => i.ProductId == product.Id && 
                                                      (i.SelectedColor == defaultColor || (i.SelectedColor == null && defaultColor == null)) && 
                                                      (i.SelectedSize == defaultSize || (i.SelectedSize == null && defaultSize == null)));

            if (item == null)
            {
                basket.Items.Add(new BasketItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.Price,
                    ImageUrl = product.ImageUrl,
                    Quantity = 1,
                    StockQuantity = product.StockQuantity,
                    AvailableColors = product.Colors,
                    AvailableSizes = product.Sizes,
                    SelectedColor = defaultColor,
                    SelectedSize = defaultSize
                });
            }
            else
            {
                item.Quantity++;
                // Ensure metadata is updated even if item already existed
                item.AvailableColors = product.Colors;
                item.AvailableSizes = product.Sizes;
            }

            var key = GetBasketKey();
            await _localStorage.SetItemAsync(key, basket);
            NotifyStateChanged();
        }

        public async Task RemoveFromBasketAsync(Guid productId)
        {
            // Remove all items with this product ID
            var basket = await GetBasketAsync();
            basket.Items.RemoveAll(i => i.ProductId == productId);
            
            var key = GetBasketKey();
            await _localStorage.SetItemAsync(key, basket);
            NotifyStateChanged();
        }

        public async Task ClearBasketAsync()
        {
            var key = GetBasketKey();
            await _localStorage.RemoveItemAsync(key);
            NotifyStateChanged();
        }

        public async Task<int> GetBasketItemCountAsync()
        {
            if (!await _authService.IsAuthenticatedAsync())
                return 0;

            var basket = await GetBasketAsync();
            return basket.TotalItems;
        }

        public async Task UpdateQuantityAsync(Guid productId, string? color, string? size, int quantity)
        {
            var basket = await GetBasketAsync();
            var item = basket.Items.FirstOrDefault(i => i.ProductId == productId && i.SelectedColor == color && i.SelectedSize == size);

            if (item != null)
            {
                if (quantity <= 0)
                {
                    basket.Items.Remove(item);
                }
                else
                {
                    item.Quantity = quantity;
                }

                var key = GetBasketKey();
                await _localStorage.SetItemAsync(key, basket);
                NotifyStateChanged();
            }
        }

        public async Task UpdateOptionsAsync(Guid productId, string? oldColor, string? oldSize, string? newColor, string? newSize)
        {
            var basket = await GetBasketAsync();
            var item = basket.Items.FirstOrDefault(i => i.ProductId == productId && i.SelectedColor == oldColor && i.SelectedSize == oldSize);

            if (item != null)
            {
                // Check if an item with the new options already exists
                var existingItem = basket.Items.FirstOrDefault(i => i.ProductId == productId && i.SelectedColor == newColor && i.SelectedSize == newSize);
                
                if (existingItem != null && existingItem != item)
                {
                    // Merge quantities
                    existingItem.Quantity += item.Quantity;
                    basket.Items.Remove(item);
                }
                else
                {
                    item.SelectedColor = newColor;
                    item.SelectedSize = newSize;
                }

                var key = GetBasketKey();
                await _localStorage.SetItemAsync(key, basket);
                NotifyStateChanged();
            }
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
