namespace TaskManagementSystem.Models
{
    public class TaskAssignment
    {
        public int TaskItemId { get; set; }   // Foreign Key to TaskItem (formerly Task)
        public int UserId { get; set; }   // Foreign Key to User
        public DateTime AssignedAt { get; set; }

        public required TaskItem TaskItem { get; set; }   // Navigation property, updated to TaskItem
        public required User User { get; set; }   // Navigation property
    }
}
