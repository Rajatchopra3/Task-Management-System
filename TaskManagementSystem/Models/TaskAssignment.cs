using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using TaskManagementSystem.Models;

public class TaskAssignment
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }   // New surrogate primary key
    public int TaskItemId { get; set; }
    public int UserId { get; set; }
    public DateTime AssignedAt { get; set; }

    public required TaskItem TaskItem { get; set; }
    public required User User { get; set; }
}
