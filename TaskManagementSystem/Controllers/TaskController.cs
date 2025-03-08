using Microsoft.AspNetCore.Authorization;  // Import Authorize attribute
using Microsoft.AspNetCore.Mvc;
using TaskManagementSystem.Models;
using TaskManagementSystem.Services;

namespace TaskManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TaskController : ControllerBase
    {
        private readonly ITaskService _taskService;

        public TaskController(ITaskService taskService)
        {
            _taskService = taskService;
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
        public async Task<ActionResult<TaskItem>> CreateTask(TaskItem taskItem)
        {
            var createdTask = await _taskService.CreateTaskAsync(taskItem);
            return CreatedAtAction(nameof(GetTaskById), new { id = createdTask.TaskItemId }, createdTask);
        }

        // PUT: api/task/{id} - Accessible to all authenticated users
        [HttpPut("{id}")]
        [Authorize]  // Ensure the user is authenticated
        public async Task<ActionResult<TaskItem>> UpdateTask(int id, TaskItem taskItem)
        {
            var updatedTask = await _taskService.UpdateTaskAsync(id, taskItem);
            if (updatedTask == null)
            {
                return NotFound();
            }
            return Ok(updatedTask);
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

        // POST: api/task/{taskItemId}/assign - Only Admins can assign tasks
        [HttpPost("{taskItemId}/assign")]
        [Authorize(Roles = "Admin")]  // Only Admin role users can assign tasks
        public async Task<ActionResult<TaskAssignment>> AssignUserToTask(int taskItemId, [FromBody] int userId)
        {
            var taskAssignment = await _taskService.AssignUserToTaskAsync(taskItemId, userId);
            return Ok(taskAssignment);
        }
    }
}
