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
                .Include(t => t.Workflow)  // Include Workflow if it exists
                .ToListAsync();
        }

        public async Task<TaskItem?> GetTaskByIdAsync(int taskId)
        {
            return await _context.Tasks
                .Include(t => t.Assignee)  // Include Assignee to get user details
                .Include(t => t.Workflow)  // Include Workflow if it exists
                .FirstOrDefaultAsync(t => t.TaskItemId == taskId);
        }

        public async Task<TaskItem> CreateTaskAsync(TaskItem taskItem)
        {
            // Check if Assignee exists
            var assignee = await _context.Users.FindAsync(taskItem.AssigneeId);
            if (assignee == null)
            {
                throw new KeyNotFoundException("Assignee not found.");
            }

            // Check if Workflow exists (if workflowId is provided)
            Workflow? workflow = null;
            if (taskItem.WorkflowId.HasValue)
            {
                workflow = await _context.Workflows.FindAsync(taskItem.WorkflowId);
                if (workflow == null)
                {
                    throw new KeyNotFoundException("Workflow not found.");
                }
            }

            // Set the relationships (Assignee and Workflow)
            taskItem.Assignee = assignee;
            taskItem.Workflow = workflow;

            // Set created and updated times
            taskItem.CreatedAt = DateTime.Now;
            taskItem.UpdatedAt = DateTime.Now;

            // Add the task to the context
            _context.Tasks.Add(taskItem);

            // Create the TaskAssignment record to link the user to the task
            var taskAssignment = new TaskAssignment
            {
                TaskItemId = taskItem.TaskItemId,  // Link to the created task
                UserId = taskItem.AssigneeId,      // The assignee user
                AssignedAt = DateTime.Now,          // Set the assignment date
                TaskItem = taskItem,
                User = assignee
            };

            // Add the TaskAssignment to the context
            _context.TaskAssignments.Add(taskAssignment);

            // Save changes to the database
            await _context.SaveChangesAsync();

            return taskItem;
        }



        public async Task<TaskItem?> UpdateTaskAsync(int taskId, TaskItem taskItem)
        {
            var existingTask = await _context.Tasks.FindAsync(taskId);
            if (existingTask == null) return null;

            // Check if the WorkflowId is valid if it's provided
            if (taskItem.WorkflowId.HasValue)
            {
                var workflow = await _context.Workflows.FindAsync(taskItem.WorkflowId.Value);
                if (workflow == null)
                {
                    throw new ArgumentException("Invalid Workflow ID.");
                }
                existingTask.Workflow = workflow;  // Update Workflow if valid
            }

            // Update other properties
            existingTask.Title = taskItem.Title;
            existingTask.Description = taskItem.Description;
            existingTask.Status = taskItem.Status;
            existingTask.DueDate = taskItem.DueDate;
            existingTask.AssigneeId = taskItem.AssigneeId;
            existingTask.WorkflowId = taskItem.WorkflowId;  // Update WorkflowId
            existingTask.UpdatedAt = DateTime.Now;

            // If the assignee has changed, update TaskAssignment
            if (existingTask.AssigneeId != taskItem.AssigneeId)
            {
                var assignee = await _context.Users.FindAsync(taskItem.AssigneeId);
                if (assignee == null)
                {
                    throw new KeyNotFoundException("Assignee not found.");
                }

                // Update the TaskAssignment
                var taskAssignment = await _context.TaskAssignments
                    .FirstOrDefaultAsync(ta => ta.TaskItemId == taskId);
                if (taskAssignment != null)
                {
                    taskAssignment.UserId = taskItem.AssigneeId;
                    taskAssignment.AssignedAt = DateTime.Now; // Re-assign the time if assignee changes
                    _context.TaskAssignments.Update(taskAssignment);
                }
                else
                {
                    // If TaskAssignment doesn't exist, create a new one
                    var newTaskAssignment = new TaskAssignment
                    {
                        TaskItemId = taskId,
                        UserId = taskItem.AssigneeId,
                        AssignedAt = DateTime.Now,
                        TaskItem = existingTask,
                        User = assignee
                    };
                    _context.TaskAssignments.Add(newTaskAssignment);
                }
            }

            // Update the task
            _context.Tasks.Update(existingTask);
            await _context.SaveChangesAsync();

            return existingTask;
        }

        public async Task<bool> DeleteTaskAsync(int taskId)
        {
            var task = await _context.Tasks.FindAsync(taskId);
            if (task == null) return false;

            // Delete related TaskAssignments first
            var taskAssignments = _context.TaskAssignments
                .Where(ta => ta.TaskItemId == taskId);

            _context.TaskAssignments.RemoveRange(taskAssignments);

            // Now delete the task
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

        public async Task<Workflow?> GetWorkflowByIdAsync(int workflowId)
        {
            return await _context.Workflows.FindAsync(workflowId);
        }

    }
}
