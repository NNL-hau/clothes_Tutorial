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

            // Use ADO.NET directly to safely check and add columns
            // (EF Core SqlQueryRaw<string> cannot map scalar primitives)
            var conn = context.Database.GetDbConnection();
            conn.Open();
            try
            {
                // Check & add Colors column
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
                        WHERE TABLE_SCHEMA = DATABASE()
                          AND TABLE_NAME = 'Products'
                          AND COLUMN_NAME = 'Colors'";
                    var count = Convert.ToInt32(cmd.ExecuteScalar());
                    if (count == 0)
                    {
                        cmd.CommandText = "ALTER TABLE Products ADD COLUMN Colors VARCHAR(500) NULL";
                        cmd.ExecuteNonQuery();
                        Console.WriteLine("[Catalog.API] Added Colors column to Products table.");
                    }
                }

                // Check & add Sizes column
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
                        WHERE TABLE_SCHEMA = DATABASE()
                          AND TABLE_NAME = 'Products'
                          AND COLUMN_NAME = 'Sizes'";
                    var count = Convert.ToInt32(cmd.ExecuteScalar());
                    if (count == 0)
                    {
                        cmd.CommandText = "ALTER TABLE Products ADD COLUMN Sizes VARCHAR(500) NULL";
                        cmd.ExecuteNonQuery();
                        Console.WriteLine("[Catalog.API] Added Sizes column to Products table.");
                    }
                }
            }
            finally
            {
                conn.Close();
            }

            // Seed Colors and Sizes for existing products if they are empty
            var products = context.Products
                .Where(p => string.IsNullOrEmpty(p.Colors) || string.IsNullOrEmpty(p.Sizes))
                .ToList();
            if (products.Any())
            {
                foreach (var p in products)
                {
                    if (string.IsNullOrEmpty(p.Colors)) p.Colors = "Đen, Trắng, Xanh, Đỏ";
                    if (string.IsNullOrEmpty(p.Sizes)) p.Sizes = "S, M, L, XL, XXL";
                }
                context.SaveChanges();
                Console.WriteLine($"[Catalog.API] Seeded Colors/Sizes for {products.Count} products.");
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
