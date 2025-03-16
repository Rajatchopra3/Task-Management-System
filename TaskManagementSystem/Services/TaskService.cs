using Microsoft.EntityFrameworkCore;
using TaskManagementSystem.Models;
using Microsoft.Extensions.Logging;

namespace TaskManagementSystem.Services
{
    public class TaskService : ITaskService
    {
        private readonly TaskManagementContext _context;
        private readonly ILogger _logger;
        public TaskService(ILogger<TaskService> logger, TaskManagementContext context)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public TaskService(TaskManagementContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            // Logger can be injected by the DI container automatically if needed.
            _logger = null!;
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
            // Ensure all required fields are set
            if (string.IsNullOrEmpty(taskItem.Title))
            {
                throw new InvalidOperationException("Title is required.");
            }
            if (string.IsNullOrEmpty(taskItem.Description))
            {
                throw new InvalidOperationException("Description is required.");
            }
            if (string.IsNullOrEmpty(taskItem.Status))
            {
                throw new InvalidOperationException("Status is required.");
            }
            if (taskItem.AssigneeId <= 0)
            {
                throw new InvalidOperationException("Valid AssigneeId is required.");
            }

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
            var existingTask = await _context.Tasks
                .Include(t => t.DependentOn) // Include dependent tasks to check their status
                .FirstOrDefaultAsync(t => t.TaskItemId == taskId);

            if (existingTask == null)
                return null;  // Return null if task doesn't exist

            // Prevent status update if task has dependencies and they are not completed
            if (taskItem.Status != existingTask.Status)
            {
                var canUpdateStatus = await CanUpdateTaskStatusAsync(taskId);  // Check if dependent tasks are completed

                if (!canUpdateStatus)
                {
                    // Log the error before throwing
                    _logger.LogError("Attempt to update task status failed. TaskId: {TaskId} - Cannot update task status because dependent tasks are not completed.", taskId);
                    throw new InvalidOperationException("Cannot update task status because dependent tasks are not completed.");
                }
            }

            // Check if the current user can change the WorkflowId
            if (taskItem.WorkflowId.HasValue)
            {
                // Only allow admins to change WorkflowId
                if (!isAdmin)
                {
                    return null;  // Only admins can change the WorkflowId
                }

                var workflow = await _context.Workflows.FindAsync(taskItem.WorkflowId.Value);
                if (workflow == null)
                {
                    // Instead of logging a warning, throw an exception if the workflow doesn't exist
                    throw new ArgumentException("Invalid Workflow ID.");
                }

                existingTask.Workflow = workflow;  // Update the Workflow if valid
            }

            // Allow updating other properties like Title, Description, and Status regardless of WorkflowId
            existingTask.Title = taskItem.Title;
            existingTask.Description = taskItem.Description;
            existingTask.Status = taskItem.Status;
            existingTask.DueDate = taskItem.DueDate;
            existingTask.UpdatedAt = DateTime.UtcNow; // Use UTC time for consistency

            // If the Task has a WorkflowId, only admins can change Assignee
            if (existingTask.WorkflowId == null || isAdmin) // If WorkflowId is null or user is admin
            {
                // If AssigneeId is different, check if the current user is the assignee or the user is an admin
                if (existingTask.AssigneeId != taskItem.AssigneeId)
                {
                    if (!isAdmin && existingTask.AssigneeId != currentUserId)
                    {
                        _logger.LogWarning("User attempted to reassign task but is not the assignee or an admin. TaskId: {TaskId} - UserId: {UserId}", taskId, currentUserId);
                        return null; // Prevent updating if the user is not the assignee or an admin
                    }

                    // Get the new assignee details
                    var assignee = await _context.Users.FindAsync(taskItem.AssigneeId);
                    if (assignee == null)
                    {
                        _logger.LogError("Assignee not found. TaskId: {TaskId} - AssigneeId: {AssigneeId}", taskId, taskItem.AssigneeId);
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
            }
            else
            {
                // If the task has a workflow, the assignee cannot be changed by anyone except admin
                _logger.LogWarning("Assignee change is not allowed because the task is part of a workflow. TaskId: {TaskId} - UserId: {UserId}", taskId, currentUserId);
                return null; // Prevent assignee change if it's part of a workflow and the user is not an admin
            }

            // Now update the task itself
            _context.Tasks.Update(existingTask);
            await _context.SaveChangesAsync();

            return existingTask;
        }

        // Helper method to check if a task's dependent tasks are completed
        private async Task<bool> CanUpdateTaskStatusAsync(int taskItemId)
        {
            // Query TaskDependencies to find tasks that the current task depends on (TaskItemId is the current task)
            var taskDependencies = await _context.TaskDependencies
                .Where(td => td.TaskItemId == taskItemId) // Only fetch dependencies where the current task is the dependent task
                .ToListAsync();

            // Check each dependency in the task chain (tasks that the current task depends on)
            foreach (var dependency in taskDependencies)
            {
                // This means the current task depends on another task (dependency.TaskItemId is the current task)
                var parentTask = await _context.Tasks
                    .FirstOrDefaultAsync(t => t.TaskItemId == dependency.DependentTaskItemId);

                // If the parent task is not completed, prevent status update
                if (parentTask != null && parentTask.Status != "Completed")
                {
                    // Log the reason why the status update is blocked
                    _logger.LogWarning("Cannot update task status. TaskId: {TaskId} has a dependency on TaskId: {DependentTaskId}, but the dependent task is not completed. ParentTaskStatus: {ParentTaskStatus}",
                        taskItemId, dependency.DependentTaskItemId, parentTask.Status);

                    return false; // Block status update if any of the parent tasks are not completed
                }
            }

            return true; // Allow status update if all dependent tasks are completed
        }




        public async Task<bool> DeleteTaskAsync(int taskId)
        {
            var task = await _context.Tasks.FindAsync(taskId);
            if (task == null) return false;

            // Check if the task is part of a workflow
            if (task.WorkflowId.HasValue)
            {
                // Fetch the task dependencies
                var taskDependencies = await _context.TaskDependencies
                    .Where(td => td.TaskItemId == taskId || td.DependentTaskItemId == taskId)
                    .ToListAsync();

                // If there are any dependencies, prevent deletion
                if (taskDependencies.Any())
                {
                    throw new InvalidOperationException("Task cannot be deleted because it has dependencies.");
                }

                // If the task has no dependencies, remove it from the workflow
                task.WorkflowId = null;
            }

            // Delete related TaskAssignments
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
