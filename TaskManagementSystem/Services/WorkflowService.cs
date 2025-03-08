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
        public async Task AddTaskToWorkflowAsync(int workflowId, TaskItem taskItem)
        {
            var workflow = await _context.Workflows.FindAsync(workflowId);
            if (workflow == null)
            {
                throw new InvalidOperationException("Workflow not found.");
            }

            taskItem.WorkflowId = workflowId; // Associate the task with the workflow
            _context.Tasks.Add(taskItem);
            await _context.SaveChangesAsync();
        }
    }
}
