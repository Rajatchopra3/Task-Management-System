using TaskManagementSystem.Models;

namespace TaskManagementSystem.Services
{
    public interface ITaskService
    {
        Task<IEnumerable<TaskItem>> GetAllTasksAsync();
        Task<TaskItem?> GetTaskByIdAsync(int taskId);
        Task<TaskItem> CreateTaskAsync(TaskItem taskItem);
        Task<TaskItem?> UpdateTaskAsync(int taskId, TaskItem taskItem);
        Task<bool> DeleteTaskAsync(int taskId);
        Task<IEnumerable<TaskAssignment>> GetTaskAssignmentsByTaskIdAsync(int taskId);
        Task<TaskAssignment?> AssignUserToTaskAsync(int taskId, int userId);
        Task<TaskItem> ReassignTaskAsync(int taskItemId, int newUserId);
        // Add this method to validate WorkflowId
        Task<Workflow?> GetWorkflowByIdAsync(int workflowId);
    }
}
