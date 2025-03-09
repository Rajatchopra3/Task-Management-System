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



        public async Task<TaskItem?> UpdateTaskAsync(int taskId, TaskItem taskItem, int currentUserId, bool isAdmin)
        {
            var existingTask = await _context.Tasks.FindAsync(taskId);
            if (existingTask == null)
                return null;  // Return null if task doesn't exist

            // Check if the WorkflowId is valid and allow update only if the user is an admin
            if (taskItem.WorkflowId.HasValue)
            {
                if (!isAdmin)
                {
                    return null;  // Only admins can change the WorkflowId
                }

                var workflow = await _context.Workflows.FindAsync(taskItem.WorkflowId.Value);
                if (workflow == null)
                {
                    throw new ArgumentException("Invalid Workflow ID.");
                }
                existingTask.Workflow = workflow;  // Update Workflow if valid
            }

            // Update other properties of the task
            existingTask.Title = taskItem.Title;
            existingTask.Description = taskItem.Description;
            existingTask.Status = taskItem.Status;
            existingTask.DueDate = taskItem.DueDate;
            existingTask.WorkflowId = taskItem.WorkflowId;
            existingTask.UpdatedAt = DateTime.Now;

            // Allow AssigneeId change only if the user is an admin or the current assignee
            if (existingTask.AssigneeId != taskItem.AssigneeId)
            {
                if (!isAdmin && existingTask.AssigneeId != currentUserId)
                {
                    return null; // Prevent updating if the user is not the assignee or an admin
                }

                // Get the new assignee details
                var assignee = await _context.Users.FindAsync(taskItem.AssigneeId);
                if (assignee == null)
                {
                    throw new KeyNotFoundException("Assignee not found.");
                }

                // Update the AssigneeId on the Task
                existingTask.AssigneeId = taskItem.AssigneeId;

                // Always create a new TaskAssignment to track the assignment history
                var newTaskAssignment = new TaskAssignment
                {
                    TaskItemId = taskId,
                    UserId = taskItem.AssigneeId,
                    AssignedAt = DateTime.UtcNow,  // Timestamp for when the assignment happened
                    TaskItem = existingTask,
                    User = assignee
                };

                // Add the new TaskAssignment
                _context.TaskAssignments.Add(newTaskAssignment);
            }

            // Now update the task itself
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

        public async Task<TaskAssignment?> AssignOrReassignUserToTaskAsync(int taskItemId, int userId)
        {
            // Find the task by taskId
            var task = await _context.Tasks.FindAsync(taskItemId);

            // Find the user by userId
            var user = await _context.Users.FindAsync(userId);

            if (task == null || user == null)
                return null;  // If task or user doesn't exist, return null

            // **Update the task's AssigneeId and UpdatedAt before saving**
            task.AssigneeId = userId;  // Update the AssigneeId to the new user
            task.UpdatedAt = DateTime.UtcNow;  // Set the UpdatedAt field to now

            // Save changes for the task (this will update the AssigneeId and UpdatedAt)
            _context.Tasks.Update(task);

            // **Always create a new TaskAssignment to allow history of assignments**
            var newAssignment = new TaskAssignment
            {
                TaskItemId = taskItemId,
                UserId = userId,
                AssignedAt = DateTime.UtcNow,  // Timestamp for when the assignment happened
                TaskItem = task,
                User = user
                // No need to set Id; it will be auto-generated by the database
            };

            // Add the new TaskAssignment to the context (this will prevent primary key violation)
            _context.TaskAssignments.Add(newAssignment);

            // Save changes to the database for both Task and TaskAssignment
            await _context.SaveChangesAsync();

            // Return the newly created TaskAssignment
            return newAssignment;
        }








        public async Task<Workflow?> GetWorkflowByIdAsync(int workflowId)
        {
            return await _context.Workflows.FindAsync(workflowId);
        }

    }
}
