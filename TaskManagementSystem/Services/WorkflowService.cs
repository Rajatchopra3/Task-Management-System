using TaskManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace TaskManagementSystem.Services
{
    public class WorkflowService : IWorkflowService
    {
        private readonly TaskManagementContext _context;

        public WorkflowService(TaskManagementContext context)
        {
            _context = context;
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
                if (await HasCyclicDependency(taskItemId, dependentTaskId.Value))
                {
                    throw new InvalidOperationException("Cyclic dependency detected.");
                }

                // Add the dependency (Task -> Dependent Task)
                var taskDependency = new TaskDependency
                {
                    TaskItemId = taskItemId,
                    DependentTaskItemId = dependentTaskId.Value
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

            // Check if the new task is part of the workflow, if not, add it
            if (newTask.WorkflowId == null)
            {
                await AddTaskToWorkflowAsync(workflowId, newTaskItemId);  // Call the method to add the new task to the workflow
            }

            // Reassign dependencies from old task to new task
            var dependentTasks = await _context.TaskDependencies
                .Where(td => td.TaskItemId == oldTaskItemId || td.DependentTaskItemId == oldTaskItemId)
                .ToListAsync();

            foreach (var dependency in dependentTasks)
            {
                if (dependency.TaskItemId == oldTaskItemId)
                {
                    dependency.TaskItemId = newTaskItemId;
                }
                else
                {
                    dependency.DependentTaskItemId = newTaskItemId;
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
            // Fetch the workflow by its ID, including tasks
            var workflow = await _context.Workflows
                .Include(w => w.Tasks)  // Include tasks in the workflow
                .FirstOrDefaultAsync(w => w.WorkflowId == workflowId);

            if (workflow == null)
            {
                throw new InvalidOperationException("Workflow not found.");
            }

            // Loop through each task in the workflow
            foreach (var task in workflow.Tasks)
            {
                // Fetch the dependencies where this task is the dependent task (TaskItemId)
                var dependentTasks = await _context.TaskDependencies
                    .Where(td => td.DependentTaskItemId == task.TaskItemId)
                    .ToListAsync();

                // Remove the dependencies where this task is the dependent task
                foreach (var dependency in dependentTasks)
                {
                    _context.TaskDependencies.Remove(dependency);
                }

                // Fetch the dependencies where this task is the task that depends on others (TaskItemId)
                var taskDependencies = await _context.TaskDependencies
                    .Where(td => td.TaskItemId == task.TaskItemId)
                    .ToListAsync();

                // Remove the dependencies where this task is the task that depends on others
                foreach (var dependency in taskDependencies)
                {
                    _context.TaskDependencies.Remove(dependency);
                }

                // Disassociate each task from the workflow by setting WorkflowId to null
                task.WorkflowId = null;
            }

            // Now remove the workflow itself
            _context.Workflows.Remove(workflow);

            // Save changes to the database
            await _context.SaveChangesAsync();
        }


        public async Task DeleteTaskFromWorkflowAsync(int workflowId, int taskItemId)
        {
            // Fetch the task by its TaskItemId
            var taskItem = await _context.Tasks
                .FirstOrDefaultAsync(t => t.TaskItemId == taskItemId && t.WorkflowId == workflowId);

            if (taskItem == null)
            {
                throw new InvalidOperationException("Task not found in this workflow.");
            }

            // Fetch all tasks that depend on this task (those that have this task as their dependent task)
            var dependentTasks = await _context.TaskDependencies
                .Where(td => td.DependentTaskItemId == taskItemId)
                .ToListAsync();

            // Create a list to track tasks to be removed from the workflow
            var tasksToRemove = new List<TaskItem>();

            // Prevent orphaned dependent tasks by checking the dependencies
            foreach (var dependency in dependentTasks)
            {
                var dependentTask = await _context.Tasks
                    .FirstOrDefaultAsync(t => t.TaskItemId == dependency.TaskItemId);

                if (dependentTask != null)
                {
                    // Check if the dependent task depends on another task
                    var furtherDependencies = await _context.TaskDependencies
                        .Where(td => td.TaskItemId == dependentTask.TaskItemId)
                        .ToListAsync();

                    if (furtherDependencies.Any())
                    {
                        // If Task 15 (dependent on Task 14) has further dependencies, 
                        // we may need to reassign it (depending on business rules)
                        // For now, let's assume we remove it from the workflow.
                        dependentTask.WorkflowId = null;
                        _context.Tasks.Update(dependentTask);
                    }
                    else
                    {
                        // If there are no further dependencies, we can safely remove it
                        tasksToRemove.Add(dependentTask);
                    }
                }

                // Remove the dependency entry from the TaskDependencies table
                _context.TaskDependencies.Remove(dependency);
            }

            // Now, remove the task itself from the workflow (disassociate)
            taskItem.WorkflowId = null;
            _context.Tasks.Update(taskItem);

            // Remove tasks that were marked for removal (e.g., Task 15 and Task 16)
            foreach (var task in tasksToRemove)
            {
                task.WorkflowId = null; // Disassociate from workflow
                _context.Tasks.Update(task);
            }

            // Save changes to the database
            await _context.SaveChangesAsync();
        }



    }
}
