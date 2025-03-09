using TaskManagementSystem.Models;

namespace TaskManagementSystem.Services
{
    public interface ITaskService
    {
        Task<IEnumerable<TaskItem>> GetAllTasksAsync();
        Task<TaskItem?> GetTaskByIdAsync(int taskId);
        Task<TaskItem> CreateTaskAsync(TaskItem taskItem);
        Task<TaskItem?> UpdateTaskAsync(int taskId, TaskItem taskItem, int currentUserId, bool isAdmin);
        Task<bool> DeleteTaskAsync(int taskId);
        Task<IEnumerable<TaskAssignment>> GetTaskAssignmentsByTaskIdAsync(int taskId);
        Task<TaskAssignment?> AssignOrReassignUserToTaskAsync(int taskId, int userId);
        // Add this method to validate WorkflowId
        Task<Workflow?> GetWorkflowByIdAsync(int workflowId);
    }
}
