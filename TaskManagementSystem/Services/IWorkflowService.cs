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
        // Optionally add a dependent task when adding a task to a workflow
        Task AddTaskToWorkflowAsync(int workflowId, int taskItemId, int? dependentTaskId = null);

        // Replace a task in the workflow with a new one (taskItemId)
        Task ReplaceTaskInWorkflowAsync(int workflowId, int oldTaskItemId, int newTaskItemId);

        // Reassign dependent tasks if a task is removed from the workflow
        Task ReassignDependentTasksAsync(int workflowId, int taskItemId, List<int> newDependencyOrder);


        // Delete a workflow by its ID
        Task DeleteWorkflowAsync(int workflowId);

        // Delete a task from a workflow using taskItemId
      
    }
}
