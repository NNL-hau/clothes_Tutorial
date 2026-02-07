
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ClothesShop.Web;
using ClothesShop.Web.Services;
using Blazored.LocalStorage;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure Named HttpClients for each API
builder.Services.AddHttpClient("IdentityApi", client => 
{ 
    client.BaseAddress = new Uri("http://localhost:5001/");
});

builder.Services.AddHttpClient("CatalogApi", client => 
{ 
    client.BaseAddress = new Uri("http://localhost:5002/");
});

builder.Services.AddHttpClient("OrderingApi", client => 
{ 
    client.BaseAddress = new Uri("http://localhost:5004/");
});

builder.Services.AddHttpClient("ReviewApi", client => 
{ 
    client.BaseAddress = new Uri("http://localhost:5006/");
});

builder.Services.AddHttpClient("PaymentApi", client => 
{ 
    client.BaseAddress = new Uri("http://localhost:5005/");
});

// Add Blazored LocalStorage
builder.Services.AddBlazoredLocalStorage();

// Register API Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductApiService, ProductApiService>();
builder.Services.AddScoped<IBannerApiService, BannerApiService>();
builder.Services.AddScoped<IOrderApiService, OrderApiService>();
builder.Services.AddScoped<IEmailService, MockEmailService>();
builder.Services.AddScoped<IUserApiService, UserApiService>();
builder.Services.AddScoped<ICategoryApiService, CategoryApiService>();
builder.Services.AddScoped<IReviewApiService, ReviewApiService>();
builder.Services.AddScoped<IPaymentApiService, PaymentApiService>();
builder.Services.AddScoped<IBasketService, BasketService>();

var host = builder.Build();

// Initialize AuthService
var authService = host.Services.GetRequiredService<IAuthService>();
await authService.InitializeAsync();

await host.RunAsync();
