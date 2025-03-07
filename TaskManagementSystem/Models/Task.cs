namespace TaskManagementSystem.Models
{
    public class Task
    {
        public int TaskId { get; set; }    // Primary Key
        public required string Title { get; set; }
        public required string Description { get; set; }
        public required string Status { get; set; } // For example: Pending, In Progress, Completed
        public int AssigneeId { get; set; } // Foreign Key to User
        public DateTime DueDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public required User Assignee { get; set; }  // Navigation property
    }
}
