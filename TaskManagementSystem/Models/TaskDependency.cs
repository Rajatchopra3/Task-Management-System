using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagementSystem.Models
{
    public class TaskDependency
    {
        [Key]
        public int TaskDependencyId { get; set; }  // Primary Key

        public int TaskItemId { get; set; }  // Task that depends on another task
        public int DependentTaskItemId { get; set; }  // Task that this task depends on

        // Navigation properties
        public TaskItem ? TaskItem { get; set; }  // Task that depends on another task
        public TaskItem ? DependentTaskItem { get; set; }  // Task that this task depends on
    }
}
