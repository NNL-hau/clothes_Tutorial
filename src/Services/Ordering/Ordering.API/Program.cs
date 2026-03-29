using Microsoft.EntityFrameworkCore;
using Ordering.API.Data;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure Database
builder.Services.AddDbContext<OrderingDbContext>(options =>
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
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Ordering API", Version = "v1" });
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
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        
        // Simple retry logic for DB connectivity in Docker
        int retries = 5;
        while (retries > 0)
        {
            try 
            {
                // Chỉ migrate các thay đổi mới, không xóa dữ liệu cũ
                db.Database.Migrate(); 
                break;
            }
            catch (Exception ex)
            {
                retries--;
                if (retries == 0) throw;
                Console.WriteLine($"[Ordering.API] Database not ready, retrying... ({5-retries}/5): {ex.Message}");
                Thread.Sleep(5000);
            }
        }
    }
}

// app.UseHttpsRedirection();

app.UseCors("AllowBlazorWasm");

app.UseAuthorization();

app.MapControllers();

app.Run();
