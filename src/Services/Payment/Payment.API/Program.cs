using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Payment.API.Data;

var builder = WebApplication.CreateBuilder(args);
Console.WriteLine("[PAYMENT_API_V2_BOOT] Application starting up...");

// Add services to the container.
builder.Services.AddControllers();

// Configure Database
builder.Services.AddDbContext<PaymentDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 33)));
});

// Configure Forwarded Headers for Docker/Nginx
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Configure Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Payment API", Version = "v1" });
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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Auto-migrate database on startup
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        
        // Simple retry logic for DB connectivity in Docker
        int retries = 5;
        while (retries > 0)
        {
            try 
            {
                db.Database.EnsureCreated();
                break;
            }
            catch (Exception ex)
            {
                retries--;
                if (retries == 0) throw;
                Console.WriteLine($"[Payment.API] Database not ready, retrying... ({5-retries}/5): {ex.Message}");
                Thread.Sleep(5000);
            }
        }
    }
}

app.UseCors("AllowBlazorWasm");

// app.UseHttpsRedirection();

app.UseForwardedHeaders();

app.UseAuthorization();

app.MapControllers();

app.Run();
