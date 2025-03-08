using TaskManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace TaskManagementSystem.Services
{
    public class UserService : IUserService
    {
        private readonly TaskManagementContext _context;

        public UserService(TaskManagementContext context)
        {
            _context = context;
        }

        public async Task<User> RegisterUserAsync(RegisterRequest registerRequest)
        {
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == registerRequest.Email);

            if (existingUser != null)
            {
                throw new InvalidOperationException("Email is already taken.");
            }

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(registerRequest.Password);

            var newUser = new User
            {
                Username = registerRequest.Username,
                Email = registerRequest.Email,
                PasswordHash = hashedPassword,
                Role = "User",  // Default role
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return newUser;
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                throw new InvalidOperationException("User not found.");
            }

            return user;
        }

    }
}
