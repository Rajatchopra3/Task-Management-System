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

            // Debugging: Output password and hash to console
            Console.WriteLine($"Input password: {loginRequest.Password}");
            Console.WriteLine($"Stored hash: {user.PasswordHash}");

            // Verify password hash
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.PasswordHash);

            // Debugging: Output the result of the verification
            Console.WriteLine($"Password verification result: {isPasswordValid}");

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
        new Claim(ClaimTypes.Role, user.Role) // If you have roles
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

    }

    public class LoginRequest
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }
}
