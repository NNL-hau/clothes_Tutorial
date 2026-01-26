
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ClothesShop.Web;
using ClothesShop.Web.Services;
using Blazored.LocalStorage;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient for Identity API
builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri("http://localhost:5001/") // Identity API URL (Matching Docker Compose port 5001)
});

// Add Blazored LocalStorage
builder.Services.AddBlazoredLocalStorage();

// Add Auth Service
builder.Services.AddScoped<IAuthService, AuthService>();

await builder.Build().RunAsync();
