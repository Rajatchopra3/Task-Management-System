namespace TaskManagementSystem.Models
{
    public class User
    {
        public int UserId { get; set; }   // Primary Key
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string PasswordHash { get; set; }
        public required string Role { get; set; }   // Admin or User
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
