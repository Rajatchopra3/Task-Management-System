using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TaskManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace TaskManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly TaskManagementContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(TaskManagementContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            // Find user by email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == loginRequest.Email);

            if (user == null)
            {
                return Unauthorized("Invalid credentials.");
            }

            // Verify password hash
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.PasswordHash);

            if (!isPasswordValid)
            {
                return Unauthorized("Invalid credentials.");
            }

            // Create JWT token
            var token = GenerateJwtToken(user);

            return Ok(new { Token = token });
        }

        private string GenerateJwtToken(User user)
        {
            var secretKey = _configuration["Jwt:SecretKey"];

            if (string.IsNullOrEmpty(secretKey))
            {
                throw new InvalidOperationException("JWT SecretKey is not configured properly.");
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest registerRequest)
        {
            // Check if the email is already taken
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == registerRequest.Email);

            if (existingUser != null)
            {
                return BadRequest("Email is already taken.");
            }

            // Hash the password
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(registerRequest.Password);

            // Create a new user with role "User" by default
            var user = new User
            {
                Username = registerRequest.Username,
                Email = registerRequest.Email,
                PasswordHash = hashedPassword,
                Role = "User",  // Default role assigned as "User"
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Add user to the database
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Return the created user (exclude password hash from response)
            return CreatedAtAction(nameof(Register), new { id = user.UserId }, new
            {
                user.UserId,
                user.Username,
                user.Email,
                user.Role,
                user.CreatedAt,
                user.UpdatedAt
            });
        }

        // GET: api/auth/me (Fetch currently authenticated user)
        [HttpGet("me")]
        [Authorize]  // Only authenticated users can access
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;  // Extract userId from the token

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not authenticated.");
            }

            // Fetch user from the database using the userId from the JWT token
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == int.Parse(userId));

            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Return user data (excluding password hash)
            return Ok(new
            {
                user.UserId,
                user.Username,
                user.Email,
                user.Role,
                user.CreatedAt,
                user.UpdatedAt
            });
        }

        // GET: api/auth/users
        [HttpGet("users")]
        [Authorize(Roles = "Admin")] // Only Admin can access this endpoint
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users.ToListAsync();

            // Exclude the password hash from the response
            var result = users.Select(user => new
            {
                user.UserId,
                user.Username,
                user.Email,
                user.Role,
                user.CreatedAt,
                user.UpdatedAt
            });

            return Ok(result);
        }

        // PUT: api/auth/update-role/{userId}
        [HttpPut("update-role/{userId}")]
        [Authorize(Roles = "Admin")] // Only Admin can access this endpoint
        public async Task<IActionResult> UpdateUserRole(int userId, [FromBody] UpdateRoleRequest updateRoleRequest)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Check if the role is valid
            if (updateRoleRequest.Role != "Admin" && updateRoleRequest.Role != "User")
            {
                return BadRequest("Invalid role.");
            }

            // Update user role
            user.Role = updateRoleRequest.Role;
            user.UpdatedAt = System.DateTime.UtcNow;

            // Save changes to the database
            await _context.SaveChangesAsync();

            return Ok(new
            {
                user.UserId,
                user.Username,
                user.Email,
                user.Role,
                user.CreatedAt,
                user.UpdatedAt
            });
        }

        // Request model for updating user role
        public class UpdateRoleRequest
        {
            public required string Role { get; set; }
        }
    }

    // Request model for login
    public class LoginRequest
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }

    
}
