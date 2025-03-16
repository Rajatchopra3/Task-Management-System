using Microsoft.AspNetCore.Authorization;  // Import Authorize attribute
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskManagementSystem.Models;
using TaskManagementSystem.Services;

namespace TaskManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TaskController : ControllerBase
    {
        private readonly ILogger<TaskController> _logger;
        private readonly ITaskService _taskService;

        // Constructor where logger and task service are injected
        public TaskController(ILogger<TaskController> logger, ITaskService taskService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger)); // Injecting the logger
            _taskService = taskService ?? throw new ArgumentNullException(nameof(taskService)); // Injecting the task service
        }

        // GET: api/task - Accessible to all authenticated users
        [HttpGet]
        [Authorize]  // Ensure the user is authenticated
        public async Task<ActionResult<IEnumerable<TaskItem>>> GetAllTasks()
        {
            var tasks = await _taskService.GetAllTasksAsync();
            return Ok(tasks);
        }

        // GET: api/task/{id} - Accessible to all authenticated users
        [HttpGet("{id}")]
        [Authorize]  // Ensure the user is authenticated
        public async Task<ActionResult<TaskItem>> GetTaskById(int id)
        {
            var task = await _taskService.GetTaskByIdAsync(id);
            if (task == null)
            {
                return NotFound();
            }
            return Ok(task);
        }

        // POST: api/task - Accessible to all authenticated users
        [HttpPost]
        [Authorize]  // Ensure the user is authenticated
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<TaskItem>> CreateTask(TaskItem taskItem)
        {
            // Optional: Validate WorkflowId if it's provided
            if (taskItem.WorkflowId.HasValue)
            {
                // Check if the WorkflowId exists in the database
                var workflow = await _taskService.GetWorkflowByIdAsync(taskItem.WorkflowId.Value);
                if (workflow == null)
                {
                    return BadRequest("Invalid Workflow ID.");
                }
            }

            var createdTask = await _taskService.CreateTaskAsync(taskItem);
            return CreatedAtAction(nameof(GetTaskById), new { id = createdTask.TaskItemId }, createdTask);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult<TaskItem>> UpdateTask(int id, TaskItem taskItem)
        {
            try
            {
                // Check if taskItem is null (validity check)
                if (taskItem == null)
                {
                    return BadRequest("Task data is missing or invalid.");
                }

                // Get the current user's ID from the claims (from JWT)
                var currentUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier); // Extracts user ID

                if (string.IsNullOrEmpty(currentUserIdClaim))
                {
                    return BadRequest("User ID is missing from the claims.");
                }

                // Parse the user ID safely
                if (!int.TryParse(currentUserIdClaim, out int currentUserId))
                {
                    return BadRequest("Invalid user ID.");
                }

                var isAdmin = User.IsInRole("Admin");

                // Call the service to update the task with user info (currentUserId and isAdmin)
                var updatedTask = await _taskService.UpdateTaskAsync(id, taskItem, currentUserId, isAdmin);

                if (updatedTask == null)
                {
                    return NotFound(new { message = "Task not found or you are not allowed to update it." });
                }

                return Ok(updatedTask);  // Return the updated task if everything was successful
            }
            catch (Exception ex)
            {
                // Log the exception details
                _logger.LogError(ex, "An error occurred while updating task with ID {TaskId}.", id);

                // Handle unexpected errors (e.g., database issues)
                return StatusCode(500, new { message = "An unexpected error occurred.", error = "Internal server error." });
            }
        }




        // DELETE: api/task/{id} - Only Admins can delete tasks
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]  // Only Admin role users can delete
        public async Task<ActionResult> DeleteTask(int id)
        {
            var success = await _taskService.DeleteTaskAsync(id);
            if (!success)
            {
                return NotFound();
            }
            return NoContent();
        }

        // GET: api/task/{taskItemId}/assignments - Accessible to all authenticated users
        [HttpGet("{taskItemId}/assignments")]
        [Authorize]  // Ensure the user is authenticated
        public async Task<ActionResult<IEnumerable<TaskAssignment>>> GetTaskAssignments(int taskItemId)
        {
            var assignments = await _taskService.GetTaskAssignmentsByTaskIdAsync(taskItemId);
            return Ok(assignments);
        }

        // POST: api/task/{taskItemId}/assign - Admins can assign or reassign tasks
        [HttpPost("{taskItemId}/assign")]
        [Authorize(Roles = "Admin")]  // Only Admin role users can assign or reassign tasks
        public async Task<ActionResult<TaskAssignment>> AssignOrReassignUserToTask(int taskItemId, [FromBody] AssignUserRequest request)
        {
            if (request == null || request.UserId <= 0)
            {
                return BadRequest("Invalid UserId.");
            }

            var taskAssignment = await _taskService.AssignOrReassignUserToTaskAsync(taskItemId, request.UserId);

            if (taskAssignment == null)
            {
                return NotFound("Task or User not found.");
            }

            return Ok(taskAssignment);
        }

    }
}
