using TaskManagementSystem.Models;

namespace TaskManagementSystem.Services
{
    public interface IUserService
    {
        Task<User> RegisterUserAsync(RegisterRequest registerRequest);
        Task<User> GetUserByEmailAsync(string email);
    }
}
