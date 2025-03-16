using TaskManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace TaskManagementSystem.Services
{
    public class WorkflowService : IWorkflowService
    {
        private readonly TaskManagementContext _context;
        private readonly ILogger<WorkflowService> _logger;

        // Constructor with both context and logger
        public WorkflowService(TaskManagementContext context, ILogger<WorkflowService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Create a new workflow
        public async Task<Workflow> CreateWorkflowAsync(Workflow workflow)
        {
            workflow.CreatedAt = DateTime.UtcNow;
            workflow.UpdatedAt = DateTime.UtcNow;
            _context.Workflows.Add(workflow);
            await _context.SaveChangesAsync();
            return workflow;
        }

        // Get all workflows
        public async Task<IEnumerable<Workflow>> GetAllWorkflowsAsync()
        {
            return await _context.Workflows.Include(w => w.Tasks).ToListAsync();
        }

        // Get a single workflow by ID
        public async Task<Workflow> GetWorkflowByIdAsync(int id)
        {
            var workflow = await _context.Workflows
                .Include(w => w.Tasks)
                .FirstOrDefaultAsync(w => w.WorkflowId == id);

            if (workflow == null)
            {
                throw new InvalidOperationException("Workflow not found.");
            }

            return workflow;
        }


        public async Task AddTaskToWorkflowAsync(int workflowId, int taskItemId, int? dependentTaskId = null)
        {
            // Fetch the workflow by its ID
            var workflow = await _context.Workflows.FindAsync(workflowId);
            if (workflow == null)
            {
                throw new InvalidOperationException("Workflow not found.");
            }

            // Fetch the task by its TaskItemId
            var taskItem = await _context.Tasks.FindAsync(taskItemId);
            if (taskItem == null)
            {
                throw new InvalidOperationException("TaskItem not found.");
            }

            // Check if the task is already part of the workflow
            if (taskItem.WorkflowId != null && taskItem.WorkflowId != workflowId)
            {
                throw new InvalidOperationException("Task is already part of another workflow.");
            }

            // Check for cyclic dependencies if a dependent task is specified
            // Check for cyclic dependencies if a dependent task is specified
            if (dependentTaskId.HasValue)
            {
                var dependentTask = await _context.Tasks.FindAsync(dependentTaskId.Value);
                if (dependentTask == null)
                {
                    throw new InvalidOperationException("Dependent task not found.");
                }

                // Ensure that the dependent task is also part of the workflow
                if (dependentTask.WorkflowId != workflowId)
                {
                    throw new InvalidOperationException("Dependent task is not part of this workflow.");
                }

                // Check for cyclic dependencies (task should not depend on itself or any task that depends on it)
                if (await HasCyclicDependency(dependentTaskId.Value, taskItemId))  // Reverse the order here
                {
                    throw new InvalidOperationException("Cyclic dependency detected.");
                }

                // Add the dependency (Task that depends -> Task being depended on)
                var taskDependency = new TaskDependency
                {
                    TaskItemId = dependentTaskId.Value,  // This task is depended on
                    DependentTaskItemId = taskItemId     // This task depends on the other
                };
                _context.TaskDependencies.Add(taskDependency);
            }


            // Associate the task with the workflow
            taskItem.WorkflowId = workflowId;

            // Save the changes to the database
            await _context.SaveChangesAsync();
        }

        // Helper method to check for cyclic dependencies
        private async Task<bool> HasCyclicDependency(int taskItemId, int dependentTaskId)
        {
            // Check if the dependent task itself depends on the current task
            var taskDependencies = await _context.TaskDependencies
                .Where(td => td.TaskItemId == dependentTaskId)
                .ToListAsync();

            foreach (var dependency in taskDependencies)
            {
                if (dependency.DependentTaskItemId == taskItemId)
                {
                    return true; // A cyclic dependency exists
                }
            }

            return false;
        }


        // Replace a task in the workflow and handle dependencies
        public async Task ReplaceTaskInWorkflowAsync(int workflowId, int oldTaskItemId, int newTaskItemId)
        {
            // Fetch the workflow
            var workflow = await _context.Workflows.FindAsync(workflowId);
            if (workflow == null)
                throw new InvalidOperationException("Workflow not found.");

            // Fetch the old and new tasks
            var oldTask = await _context.Tasks.FindAsync(oldTaskItemId);
            var newTask = await _context.Tasks.FindAsync(newTaskItemId);

            // Check if both tasks exist
            if (oldTask == null || newTask == null)
            {
                throw new InvalidOperationException("One or both tasks not found.");
            }

            // Ensure the old task is part of the workflow
            if (oldTask.WorkflowId != workflowId)
            {
                throw new InvalidOperationException("Old task is not part of the workflow.");
            }

            // Ensure the new task is not already assigned to another workflow
            if (newTask.WorkflowId != null && newTask.WorkflowId != workflowId)
            {
                throw new InvalidOperationException("New task is already assigned to a different workflow.");
            }

            // If the new task is not part of the workflow, add it
            if (newTask.WorkflowId == null)
            {
                await AddTaskToWorkflowAsync(workflowId, newTaskItemId);  // This method adds the new task to the workflow
            }

            // Reassign dependencies from old task to new task
            var dependentTasks = await _context.TaskDependencies
                .Where(td => td.TaskItemId == oldTaskItemId || td.DependentTaskItemId == oldTaskItemId)
                .ToListAsync();

            foreach (var dependency in dependentTasks)
            {
                if (dependency.TaskItemId == oldTaskItemId)
                {
                    dependency.TaskItemId = newTaskItemId;  // Reassign the dependent task
                }
                else
                {
                    dependency.DependentTaskItemId = newTaskItemId;  // Reassign the task being depended on
                }
            }

            // Remove the old task from the workflow
            oldTask.WorkflowId = null;

            // Save changes
            await _context.SaveChangesAsync();
        }




        // Reassign dependent tasks if a task is deleted
        public async Task ReassignDependentTasksAsync(int workflowId, int taskItemId, List<int> newDependencyOrder)
        {
            // Fetch the task by its TaskItemId and ensure it's part of the specified workflow
            var taskItem = await _context.Tasks
                .FirstOrDefaultAsync(t => t.TaskItemId == taskItemId && t.WorkflowId == workflowId);

            if (taskItem == null)
            {
                throw new InvalidOperationException("Task not found in this workflow.");
            }

            // Step 1: Handle tasks that are not part of the workflow
            var tasksToAdd = new List<TaskItem>();
            foreach (var taskId in newDependencyOrder)
            {
                // If the task is not part of the workflow, attempt to add it
                var task = await _context.Tasks
                    .FirstOrDefaultAsync(t => t.TaskItemId == taskId && t.WorkflowId == workflowId);

                if (task == null)
                {
                    // Task is not in the workflow, so call the AddTaskToWorkflowAsync method to add it
                    await AddTaskToWorkflowAsync(workflowId, taskId); // This calls your existing method
                }
                else
                {
                    tasksToAdd.Add(task);  // Ensure task is not null before adding it to the list
                }
            }

            // Fetch all dependencies related to this task (both incoming and outgoing)
            var taskDependencies = await _context.TaskDependencies
                .Where(td => td.TaskItemId == taskItemId || td.DependentTaskItemId == taskItemId)
                .ToListAsync();

            // Step 2: Remove the current dependencies (taskItemId related)
            foreach (var dependency in taskDependencies)
            {
                _context.TaskDependencies.Remove(dependency);
            }

            // Step 3: Update task dependencies according to the new dependency order
            for (int i = 0; i < newDependencyOrder.Count - 1; i++)
            {
                var currentTask = newDependencyOrder[i];
                var nextTask = newDependencyOrder[i + 1];

                var taskDependency = new TaskDependency
                {
                    TaskItemId = currentTask,
                    DependentTaskItemId = nextTask
                };

                _context.TaskDependencies.Add(taskDependency);
            }

            // Step 4: Handle tasks that are removed from the workflow (i.e., those not in the new dependency order)
            var tasksToRemoveFromWorkflow = await _context.Tasks
                .Where(t => t.WorkflowId == workflowId && !newDependencyOrder.Contains(t.TaskItemId))
                .ToListAsync();

            foreach (var task in tasksToRemoveFromWorkflow)
            {
                task.WorkflowId = null;  // Disassociate from workflow
                _context.Tasks.Update(task);
            }

            // Step 5: Save changes to the database
            await _context.SaveChangesAsync();
        }






        // Delete a workflow by ID
        public async Task DeleteWorkflowAsync(int workflowId)
        {
            // Fetch the workflow by its ID, including tasks and task dependencies
            var workflow = await _context.Workflows
                .Include(w => w.Tasks)  // Include tasks in the workflow
                .FirstOrDefaultAsync(w => w.WorkflowId == workflowId);

            if (workflow == null)
            {
                throw new InvalidOperationException("Workflow not found.");
            }

            // Fetch all dependencies related to tasks in the workflow (both incoming and outgoing)
            var taskDependencies = await _context.TaskDependencies
                .Where(td => workflow.Tasks.Select(t => t.TaskItemId).Contains(td.TaskItemId) ||
                            workflow.Tasks.Select(t => t.TaskItemId).Contains(td.DependentTaskItemId))
                .ToListAsync();

            // Remove all dependencies related to tasks in the workflow
            _context.TaskDependencies.RemoveRange(taskDependencies);

            // Disassociate each task from the workflow
            foreach (var task in workflow.Tasks)
            {
                task.WorkflowId = null;  // Remove the task from the workflow
            }

            // Now remove the workflow itself
            _context.Workflows.Remove(workflow);

            // Save changes to the database
            await _context.SaveChangesAsync();
        }






    }
}
