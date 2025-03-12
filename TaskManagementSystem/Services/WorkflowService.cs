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


        // Add task to a workflow
        public async Task AddTaskToWorkflowAsync(int workflowId, int taskItemId)
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

            // Associate the task with the workflow
            taskItem.WorkflowId = workflowId;

            // Save the changes to the database
            await _context.SaveChangesAsync();
        }

        // Delete a workflow by ID
        public async Task DeleteWorkflowAsync(int workflowId)
        {
            // Fetch the workflow by its ID
            var workflow = await _context.Workflows
                .Include(w => w.Tasks)  // Include tasks to remove associations
                .FirstOrDefaultAsync(w => w.WorkflowId == workflowId);

            if (workflow == null)
            {
                throw new InvalidOperationException("Workflow not found.");
            }

            // Remove all task associations by setting the WorkflowId to null for all tasks
            foreach (var task in workflow.Tasks)
            {
                task.WorkflowId = null;  // Disassociate task from workflow
            }

            // Remove the workflow itself
            _context.Workflows.Remove(workflow);

            // Save changes to the database
            await _context.SaveChangesAsync();
        }

        // Delete a task from a workflow
        public async Task DeleteTaskFromWorkflowAsync(int workflowId, int taskItemId)
        {
            // Fetch the task by its TaskItemId
            var taskItem = await _context.Tasks
                .FirstOrDefaultAsync(t => t.TaskItemId == taskItemId && t.WorkflowId == workflowId);

            if (taskItem == null)
            {
                throw new InvalidOperationException("Task not found in this workflow.");
            }

            // Disassociate the task from the workflow by setting WorkflowId to null
            taskItem.WorkflowId = null;

            // Save changes to the database
            await _context.SaveChangesAsync();
        }

    }
}
