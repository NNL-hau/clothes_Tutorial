using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Identity.API.Data;
using Identity.API.DTOs;
using Identity.API.Models;
using Identity.API.Services;
using BCrypt.Net;

namespace Identity.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IdentityDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthController> _logger;
        
        public AuthController(
            IdentityDbContext context,
            IJwtService jwtService,
            ILogger<AuthController> logger)
        {
            _context = context;
            _jwtService = jwtService;
            _logger = logger;
        }
        
        /// <summary>
        /// Register a new customer
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            try
            {
                // Check if user already exists
                if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                {
                    return BadRequest(new { message = "Email already registered" });
                }
                
                // Create new user
                var user = new User
                {
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    FullName = request.FullName,
                    PhoneNumber = request.PhoneNumber,
                    Address = request.Address,
                    Role = "Customer" // Default role
                };
                
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                
                // Generate JWT token
                var token = _jwtService.GenerateToken(user);
                
                return Ok(new AuthResponse
                {
                    UserId = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    Role = user.Role,
                    Token = token
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                return StatusCode(500, new { message = "An error occurred during registration" });
            }
        }
        
        /// <summary>
        /// Login with email and password
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                // Find user by email
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                
                if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    return Unauthorized(new { message = "Invalid email or password" });
                }
                
                // Generate JWT token
                var token = _jwtService.GenerateToken(user);
                
                return Ok(new AuthResponse
                {
                    UserId = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    Role = user.Role,
                    Token = token
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, new { message = "An error occurred during login" });
            }
        }
        
        /// <summary>
        /// Get current user information (requires authentication)
        /// </summary>
        [HttpGet("me")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<ActionResult<UserResponse>> GetCurrentUser()
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized();
                }
                
                var user = await _context.Users.FindAsync(userId);
                
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }
                
                return Ok(new UserResponse
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    Role = user.Role,
                    PhoneNumber = user.PhoneNumber,
                    Address = user.Address,
                    CreatedAt = user.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }
    }
}
