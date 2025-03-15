using System.ComponentModel.DataAnnotations;

namespace TaskManagementSystem.Models
{
    public class TaskItem
    {
        [Key]
        public int TaskItemId { get; set; }  // Primary Key

        // Ensure these properties are non-nullable
        public required string Title { get; set; }
        public required string Description { get; set; }
        public required string Status { get; set; }  // For example: Pending, In Progress, Completed

        public required int AssigneeId { get; set; }  // Foreign Key to User
        public int? WorkflowId { get; set; }  // Foreign Key to Workflow (New)

        public DateTime DueDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // RowVersion for concurrency control
        [Timestamp]
        public byte[]? RowVersion { get; set; }  // This will be managed by EF Core

        // Navigation properties
        public User? Assignee { get; set; }  // Navigation property to User
        public Workflow? Workflow { get; set; }  // Navigation property to Workflow

        public ICollection<TaskDependency> Dependencies { get; set; } = new List<TaskDependency>();  // Tasks that depend on this task
        public ICollection<TaskDependency> DependentOn { get; set; } = new List<TaskDependency>();  // Tasks that this task depends on
    }
}
