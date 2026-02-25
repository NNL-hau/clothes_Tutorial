using Microsoft.EntityFrameworkCore;
using Review.API.Data;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure Database
builder.Services.AddDbContext<ReviewDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0)));
});

// Configure Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Review API", Version = "v1" });
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
}

// Initialize Database (runs in ALL environments including Docker)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ReviewDbContext>();
    
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
            Console.WriteLine($"[Review.API] Database not ready, retrying... ({5-retries}/5): {ex.Message}");
            Thread.Sleep(5000);
        }
    }
}

app.UseCors("AllowBlazorWasm");

// app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
