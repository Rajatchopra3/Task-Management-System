using Microsoft.EntityFrameworkCore;
using TaskManagementSystem.Models;


namespace TaskManagementSystem.Services
{
    public class TaskService : ITaskService
    {
        private readonly TaskManagementContext _context;

        public TaskService(TaskManagementContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<TaskItem>> GetAllTasksAsync()
        {
            return await _context.Tasks
                .Include(t => t.Assignee)  // Include Assignee to get user details
                .ToListAsync();
        }

        public async Task<TaskItem?> GetTaskByIdAsync(int taskId)
        {
            return await _context.Tasks
                .Include(t => t.Assignee)  // Include Assignee to get user details
                .FirstOrDefaultAsync(t => t.TaskItemId == taskId);
        }

        public async Task<TaskItem> CreateTaskAsync(TaskItem taskItem)
        {
            taskItem.CreatedAt = DateTime.Now;
            taskItem.UpdatedAt = DateTime.Now;

            _context.Tasks.Add(taskItem);
            await _context.SaveChangesAsync();

            return taskItem;
        }

        public async Task<TaskItem?> UpdateTaskAsync(int taskId, TaskItem taskItem)
        {
            var existingTask = await _context.Tasks.FindAsync(taskId);
            if (existingTask == null) return null;

            existingTask.Title = taskItem.Title;
            existingTask.Description = taskItem.Description;
            existingTask.Status = taskItem.Status;
            existingTask.DueDate = taskItem.DueDate;
            existingTask.AssigneeId = taskItem.AssigneeId;
            existingTask.UpdatedAt = DateTime.Now;

            _context.Tasks.Update(existingTask);
            await _context.SaveChangesAsync();

            return existingTask;
        }


        public async Task<bool> DeleteTaskAsync(int taskId)
        {
            var task = await _context.Tasks.FindAsync(taskId);
            if (task == null) return false;

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<TaskAssignment>> GetTaskAssignmentsByTaskIdAsync(int taskId)
        {
            return await _context.TaskAssignments
                .Where(ta => ta.TaskItemId == taskId)
                .Include(ta => ta.User)  // Include the assigned user details
                .ToListAsync();
        }

        public async Task<TaskAssignment?> AssignUserToTaskAsync(int taskId, int userId)
        {
            var task = await _context.Tasks.FindAsync(taskId);
            var user = await _context.Users.FindAsync(userId);

            if (task == null || user == null) return null;

            var taskAssignment = new TaskAssignment
            {
                TaskItemId = taskId,
                UserId = userId,
                TaskItem = task,  // Set the required navigation property TaskItem
                User = user,      // Set the required navigation property User
                AssignedAt = DateTime.Now
            };

            _context.TaskAssignments.Add(taskAssignment);
            await _context.SaveChangesAsync();

            return taskAssignment;
        }

        public async Task<TaskItem> ReassignTaskAsync(int taskItemId, int newUserId)
        {
            var taskItem = await _context.Tasks
                .FirstOrDefaultAsync(t => t.TaskItemId == taskItemId);

            if (taskItem == null)
            {
                throw new KeyNotFoundException("Task not found.");
            }

            // Fetch the new assignee user
            var newAssignee = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == newUserId);

            if (newAssignee == null)
            {
                throw new InvalidOperationException("User not found.");
            }

            // Reassign the task to the new user
            taskItem.AssigneeId = newUserId;
            taskItem.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return taskItem;
        }

    }
}
