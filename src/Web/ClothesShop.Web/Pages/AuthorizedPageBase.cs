using Microsoft.AspNetCore.Components;
using ClothesShop.Web.Services;

namespace ClothesShop.Web.Pages
{
    public abstract class AuthorizedPageBase : ComponentBase
    {
        [Inject] protected IAuthService AuthService { get; set; } = default!;
        [Inject] protected NavigationManager Navigation { get; set; } = default!;

        protected override async Task OnInitializedAsync()
        {
            var isAuthenticated = await AuthService.IsAuthenticatedAsync();
            
            if (!isAuthenticated)
            {
                Navigation.NavigateTo("/login");
                return;
            }

            var role = await AuthService.GetUserRoleAsync();
            
            // Check if requires admin role
            if (RequireAdminRole && role != "Admin")
            {
                Navigation.NavigateTo("/login");
                return;
            }

            await base.OnInitializedAsync();
        }

        // Override this in derived classes if admin role is required
        protected virtual bool RequireAdminRole => true;
    }
}
