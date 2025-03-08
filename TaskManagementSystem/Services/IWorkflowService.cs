using TaskManagementSystem.Models;

namespace TaskManagementSystem.Services
{
    public interface IWorkflowService
    {
        Task<Workflow> CreateWorkflowAsync(Workflow workflow);
        Task<IEnumerable<Workflow>> GetAllWorkflowsAsync();
        Task<Workflow> GetWorkflowByIdAsync(int id);
        Task AddTaskToWorkflowAsync(int workflowId, TaskItem taskItem);
    }
}
