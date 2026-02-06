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
            var item = basket.Items.FirstOrDefault(i => i.ProductId == product.Id);

            if (item == null)
            {
                basket.Items.Add(new BasketItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.Price,
                    ImageUrl = product.ImageUrl,
                    Quantity = 1,
                    StockQuantity = product.StockQuantity
                });
            }
            else
            {
                item.Quantity++;
            }

            var key = GetBasketKey();
            await _localStorage.SetItemAsync(key, basket);
            NotifyStateChanged();
        }

        public async Task RemoveFromBasketAsync(Guid productId)
        {
            var basket = await GetBasketAsync();
            var item = basket.Items.FirstOrDefault(i => i.ProductId == productId);

            if (item != null)
            {
                basket.Items.Remove(item);
                var key = GetBasketKey();
                await _localStorage.SetItemAsync(key, basket);
                NotifyStateChanged();
            }
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

        public async Task UpdateQuantityAsync(Guid productId, int quantity)
        {
            var basket = await GetBasketAsync();
            var item = basket.Items.FirstOrDefault(i => i.ProductId == productId);

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

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
