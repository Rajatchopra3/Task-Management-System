using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagementSystem.Models;
using TaskManagementSystem.Services;

namespace TaskManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WorkflowController : ControllerBase
    {
        private readonly IWorkflowService _workflowService;

        public WorkflowController(IWorkflowService workflowService)
        {
            _workflowService = workflowService;
        }

        // POST: api/workflows
        [HttpPost]
        [Authorize(Roles = "Admin")] // Only Admin can create workflows
        public async Task<ActionResult<Workflow>> CreateWorkflow([FromBody] Workflow workflow)
        {
            Console.WriteLine("CreateWorkflow method has been reached.");
            if (workflow == null)
            {
                return BadRequest("Workflow data is required.");
            }

            var createdWorkflow = await _workflowService.CreateWorkflowAsync(workflow);
            return CreatedAtAction(nameof(GetWorkflowById), new { id = createdWorkflow.WorkflowId }, createdWorkflow);
        }

        // GET: api/workflows
        [HttpGet]
        [Authorize(Roles = "Admin,User")] // Anyone with valid role can view workflows
        public async Task<ActionResult<IEnumerable<Workflow>>> GetAllWorkflows()
        {
            var workflows = await _workflowService.GetAllWorkflowsAsync();
            return Ok(workflows);
        }

        // GET: api/workflows/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,User")] // Anyone with valid role can view workflows
        public async Task<ActionResult<Workflow>> GetWorkflowById(int id)
        {
            var workflow = await _workflowService.GetWorkflowByIdAsync(id);
            if (workflow == null)
            {
                return NotFound();
            }
            return Ok(workflow);
        }

        // POST: api/workflows/{workflowId}/tasks/{taskItemId}
        // POST: api/workflows/{workflowId}/tasks/{taskItemId}/dependency
        [HttpPost("{workflowId}/tasks/{taskItemId}")]
        [Authorize(Roles = "Admin")] // Only Admin can add tasks to workflows
        public async Task<ActionResult> AddTaskToWorkflow(int workflowId, int taskItemId, [FromQuery] int? dependentTaskId = null)
        {
            if (taskItemId <= 0)
            {
                return BadRequest("Invalid TaskItem ID.");
            }

            try
            {
                // If dependentTaskId is provided, include it while adding the task to the workflow
                await _workflowService.AddTaskToWorkflowAsync(workflowId, taskItemId, dependentTaskId);
                return NoContent();  // Successful request, no content to return
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);  // Return NotFound if workflow or task is not found
            }
        }

        // POST: api/workflows/{workflowId}/tasks/{oldTaskItemId}/replace/{newTaskItemId}
        [HttpPost("{workflowId}/tasks/{oldTaskItemId}/replace/{newTaskItemId}")]
        [Authorize(Roles = "Admin")] // Only Admin can replace tasks in workflows
        public async Task<ActionResult> ReplaceTaskInWorkflow(int workflowId, int oldTaskItemId, int newTaskItemId)
        {
            if (oldTaskItemId <= 0 || newTaskItemId <= 0)
            {
                return BadRequest("Invalid TaskItem IDs.");
            }

            try
            {
                // Replacing the task in the workflow
                await _workflowService.ReplaceTaskInWorkflowAsync(workflowId, oldTaskItemId, newTaskItemId);
                return NoContent();  // Successful request, no content to return
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);  // Return NotFound if workflow or task is not found
            }
        }

        // POST: api/workflows/{workflowId}/tasks/{taskItemId}/reassign-dependents
        [HttpPost("{workflowId}/tasks/{taskItemId}/reassign-dependents")]
        [Authorize(Roles = "Admin")] // Only Admin can reassign dependent tasks
        public async Task<ActionResult> ReassignDependentTasks(int workflowId, int taskItemId, [FromBody] List<int> newDependencyOrder)
        {
            if (taskItemId <= 0)
            {
                return BadRequest("Invalid TaskItem ID.");
            }

            if (newDependencyOrder == null || newDependencyOrder.Count == 0)
            {
                return BadRequest("New dependency order is required.");
            }

            try
            {
                // Reassigning dependent tasks for the specified task
                await _workflowService.ReassignDependentTasksAsync(workflowId, taskItemId, newDependencyOrder);
                return NoContent();  // Successful request, no content to return
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);  // Return NotFound if task or workflow is not found
            }
        }


        // DELETE: api/workflows/{workflowId}
        [HttpDelete("{workflowId}")]
        [Authorize(Roles = "Admin")] // Only Admin can delete workflows
        public async Task<ActionResult> DeleteWorkflow(int workflowId)
        {
            try
            {
                await _workflowService.DeleteWorkflowAsync(workflowId);
                return NoContent();  // Successful deletion, no content to return
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);  // Return NotFound if the workflow is not found
            }
        }

       
    }
}
