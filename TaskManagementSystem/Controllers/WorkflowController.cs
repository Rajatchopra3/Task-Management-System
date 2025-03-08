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

        // POST: api/workflows/{workflowId}/tasks
        [HttpPost("{workflowId}/tasks")]
        [Authorize(Roles = "Admin")] // Only Admin can add tasks to workflows
        public async Task<ActionResult> AddTaskToWorkflow(int workflowId, [FromBody] TaskItem taskItem)
        {
            if (taskItem == null)
            {
                return BadRequest("Task data is required.");
            }

            await _workflowService.AddTaskToWorkflowAsync(workflowId, taskItem);
            return NoContent();  // Successful request, but no content to return
        }
    }
}
