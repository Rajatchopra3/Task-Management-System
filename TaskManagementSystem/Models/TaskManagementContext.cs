using Microsoft.EntityFrameworkCore;

namespace TaskManagementSystem.Models
{
    // This class inherits from DbContext, enabling interaction with the database.
    public class TaskManagementContext : DbContext
    {
        // Constructor that passes options to the base class (DbContext).
        public TaskManagementContext(DbContextOptions<TaskManagementContext> options)
            : base(options)
        {
        }

        // DbSet properties represent tables in the database
        public DbSet<Task> Tasks { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Workflow> Workflows { get; set; }
        public DbSet<TaskAssignment> TaskAssignments { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        // OnModelCreating is a method to configure entity relationships, constraints, etc.
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TaskAssignment>()
        .HasKey(t => new { t.TaskId, t.UserId });
        }
    }
}
