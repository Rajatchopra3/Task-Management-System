using TaskManagementSystem.Models;

namespace TaskManagementSystem.Services
{
    public interface IWorkflowService
    {
        // Create a new workflow
        Task<Workflow> CreateWorkflowAsync(Workflow workflow);

        // Get all workflows
        Task<IEnumerable<Workflow>> GetAllWorkflowsAsync();

        // Get a single workflow by ID
        Task<Workflow> GetWorkflowByIdAsync(int id);

        // Add a task to a workflow (using taskItemId instead of the whole taskItem object)
        Task AddTaskToWorkflowAsync(int workflowId, int taskItemId);

        // Delete a workflow by its ID
        Task DeleteWorkflowAsync(int workflowId);

        // Delete a task from a workflow using taskItemId
        Task DeleteTaskFromWorkflowAsync(int workflowId, int taskItemId);
    }
}
