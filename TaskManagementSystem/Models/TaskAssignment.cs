namespace TaskManagementSystem.Models
{
    public class TaskAssignment
    {
        public int TaskId { get; set; }   // Foreign Key to Task
        public int UserId { get; set; }   // Foreign Key to User
        public DateTime AssignedAt { get; set; }

        public required Task Task { get; set; }   // Navigation property
        public required User User { get; set; }   // Navigation property
    }
}
