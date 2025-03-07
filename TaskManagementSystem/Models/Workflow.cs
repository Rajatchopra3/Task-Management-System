namespace TaskManagementSystem.Models
{
    public class Workflow
    {
        public int WorkflowId { get; set; }   // Primary Key
        public required string Name { get; set; }
        public required string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
