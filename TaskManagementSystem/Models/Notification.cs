namespace TaskManagementSystem.Models
{
    public class Notification
    {
        public int NotificationId { get; set; }  // Primary Key
        public int UserId { get; set; }          // Foreign Key to User
        public required string Type { get; set; }         // For example: Task Update, Task Assignment
        public required string Message { get; set; }
        public required string Status { get; set; }       // For example: Read, Unread
        public DateTime CreatedAt { get; set; }

        public required User User { get; set; }   // Navigation property
    }
}
