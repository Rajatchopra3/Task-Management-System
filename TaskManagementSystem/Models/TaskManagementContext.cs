using Microsoft.EntityFrameworkCore;

namespace TaskManagementSystem.Models
{
    public class TaskManagementContext : DbContext
    {
        public TaskManagementContext(DbContextOptions<TaskManagementContext> options)
            : base(options)
        {
        }

        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Workflow> Workflows { get; set; }
        public DbSet<TaskAssignment> TaskAssignments { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Define the primary key if it's not using [Key] attribute
            modelBuilder.Entity<TaskItem>()
                .HasKey(t => t.TaskItemId);

            modelBuilder.Entity<TaskAssignment>()
                .HasKey(t => new { t.TaskItemId, t.UserId });
        }
    }
}
