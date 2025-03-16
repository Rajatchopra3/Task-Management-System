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

        // New DbSet for TaskDependency
        public DbSet<TaskDependency> TaskDependencies { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Define the primary key for TaskItem if it's not using [Key] attribute
            modelBuilder.Entity<TaskItem>()
                .HasKey(t => t.TaskItemId);

            // Define composite primary key for TaskAssignment (TaskItemId and UserId)
            modelBuilder.Entity<TaskAssignment>()
                .HasKey(t => new { t.TaskItemId, t.UserId });

            // Define the primary key for TaskDependency
            modelBuilder.Entity<TaskDependency>()
                .HasKey(t => t.TaskDependencyId);  // Make sure TaskDependency has its primary key set

            // Configure the many-to-many relationship between TaskItem and TaskDependency
            modelBuilder.Entity<TaskDependency>()
                .HasOne(td => td.TaskItem)
                .WithMany(t => t.DependentOn)  // Correct property name: DependentOn
                .HasForeignKey(td => td.TaskItemId)
                .OnDelete(DeleteBehavior.Cascade); // Optional: this deletes dependencies when the task is deleted

            modelBuilder.Entity<TaskDependency>()
                .HasOne(td => td.DependentTaskItem)
                .WithMany(t => t.Dependencies)  // Correct property name: Dependencies
                .HasForeignKey(td => td.DependentTaskItemId)
                .OnDelete(DeleteBehavior.Cascade); // Optional: this deletes dependencies when the task is deleted
        }



    }
}
