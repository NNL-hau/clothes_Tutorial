using Microsoft.EntityFrameworkCore;
using Catalog.API.Data;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHttpClient();

// Configure Database
builder.Services.AddDbContext<CatalogDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0)));
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorWasm", policy =>
    {
        policy.SetIsOriginAllowed(origin => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Configure Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Catalog API", Version = "v1" });
});

var app = builder.Build();

// Initialize Database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<CatalogDbContext>();
    
    // Simple retry logic for DB connectivity in Docker
    int retries = 5;
    while (retries > 0)
    {
        try 
        {
            context.Database.EnsureCreated();
            
            // Seed Colors and Sizes for existing products if they are empty
            var products = context.Products.Where(p => string.IsNullOrEmpty(p.Colors) || string.IsNullOrEmpty(p.Sizes)).ToList();
            if (products.Any())
            {
                foreach (var p in products)
                {
                    p.Colors = "Đen, Trắng, Xanh, Đỏ";
                    p.Sizes = "S, M, L, XL, XXL";
                }
                context.SaveChanges();
            }
            
            break;
        }
        catch (Exception ex)
        {
            retries--;
            if (retries == 0)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred while creating the database after multiple retries.");
                throw;
            }
            Console.WriteLine($"[Catalog.API] Database not ready, retrying... ({5-retries}/5): {ex.Message}");
            Thread.Sleep(5000);
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.UseStaticFiles(); // Serve static files from wwwroot

app.UseCors("AllowBlazorWasm");

app.UseAuthorization();

app.MapControllers();

app.Run();
