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
        [HttpPost("{workflowId}/tasks/{taskItemId}")]
        [Authorize(Roles = "Admin")] // Only Admin can add tasks to workflows
        public async Task<ActionResult> AddTaskToWorkflow(int workflowId, int taskItemId)
        {
            if (taskItemId <= 0)
            {
                return BadRequest("Invalid TaskItem ID.");
            }

            try
            {
                await _workflowService.AddTaskToWorkflowAsync(workflowId, taskItemId);
                return NoContent();  // Successful request, no content to return
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);  // Return NotFound if workflow or task is not found
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

        // DELETE: api/workflows/{workflowId}/tasks/{taskItemId}
        [HttpDelete("{workflowId}/tasks/{taskItemId}")]
        [Authorize(Roles = "Admin")] // Only Admin can delete tasks from workflows
        public async Task<ActionResult> DeleteTaskFromWorkflow(int workflowId, int taskItemId)
        {
            try
            {
                await _workflowService.DeleteTaskFromWorkflowAsync(workflowId, taskItemId);
                return NoContent();  // Successful deletion, no content to return
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);  // Return NotFound if task or workflow is not found
            }
        }


    }
}
