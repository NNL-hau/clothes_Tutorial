using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Identity.API.Data;
using Identity.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Configure Database
builder.Services.AddDbContext<IdentityDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0)));
});

// Configure JWT
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ClothesECommerce";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "ClothesECommerceUsers";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
    };
});

builder.Services.AddAuthorization();

// Add JWT Service
// Add JWT Service
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IGoogleAuthService, GoogleAuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Configure Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Identity API", Version = "v1" });
    
    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(origin => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Ensure database is created and seed default admin
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    // Simple retry logic for DB connectivity in Docker
    int retries = 5;
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    while (retries > 0)
    {
        try 
        {
            context.Database.EnsureCreated();

            // Handle schema update for new OTP columns (since EnsureCreated doesn't handle existing tables)
            try 
            {
                var conn = context.Database.GetDbConnection();
                if (conn.State != System.Data.ConnectionState.Open) conn.Open();
                using var command = conn.CreateCommand();
                
                // Add ResetOtp
                try {
                    command.CommandText = "ALTER TABLE Users ADD COLUMN ResetOtp VARCHAR(256);";
                    command.ExecuteNonQuery();
                } catch { /* Column might already exist */ }

                // Add ResetOtpExpiry
                try {
                    command.CommandText = "ALTER TABLE Users ADD COLUMN ResetOtpExpiry DATETIME;";
                    command.ExecuteNonQuery();
                } catch { /* Column might already exist */ }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error while ensuring columns exist. This is expected if they already exist.");
            }

            // Ensure default admin account exists with correct credentials
            var existingAdmin = context.Users.FirstOrDefault(u => u.FullName == "admin" || u.Role == "Admin");
            if (existingAdmin == null)
            {
                // No admin at all → create one
                context.Users.Add(new Identity.API.Models.User
                {
                    Id = Guid.NewGuid(),
                    FullName = "admin",
                    Email = "admin@canifas.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    Role = "Admin",
                    CreatedAt = DateTime.UtcNow
                });
                context.SaveChanges();
                logger.LogInformation("Default admin created: username='admin', password='Admin@123'");
            }
            else
            {
                // Always reset admin credentials to ensure correct password
                existingAdmin.FullName = "admin";
                existingAdmin.Email = "admin@canifas.com";
                existingAdmin.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
                existingAdmin.Role = "Admin";
                context.SaveChanges();
                logger.LogInformation("Admin credentials reset: username='admin', password='Admin@123'");
            }

            break;
        }
        catch (Exception ex)
        {
            retries--;
            if (retries == 0)
            {
                logger.LogError(ex, "An error occurred creating the DB after multiple retries.");
                throw;
            }
            Console.WriteLine($"[Identity.API] Database not ready, retrying... ({5 - retries}/5): {ex.Message}");
            Thread.Sleep(5000);
        }
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
