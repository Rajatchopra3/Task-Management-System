using Moq;
using TaskManagementSystem.Models;
using TaskManagementSystem.Services;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Xunit;
using System;
using System.Threading.Tasks;

namespace TaskManagementSystem.Tests
{
    public class WorkflowServiceTests : IDisposable
    {
        private readonly Mock<ILogger<WorkflowService>> _mockLogger;
        private readonly TaskManagementContext _context;
        private readonly WorkflowService _workflowService;

        public WorkflowServiceTests()
        {
            _mockLogger = new Mock<ILogger<WorkflowService>>();

            // Set up in-memory database for testing
            var options = new DbContextOptionsBuilder<TaskManagementContext>()
                            .UseInMemoryDatabase(databaseName: "TestWorkflowDatabase")
                            .Options;
            _context = new TaskManagementContext(options);

            // Create the WorkflowService with the in-memory context and logger
            _workflowService = new WorkflowService(_context, _mockLogger.Object);

            // Clear the database at the start of each test
            _context.Database.EnsureDeleted();   // Delete the database if it exists
            _context.Database.EnsureCreated();   // Create a new clean database
        }

        [Fact]
        public async Task CreateWorkflowAsync_ShouldCreateWorkflow()
        {
            // Arrange
            var workflow = new Workflow
            {
                Name = "New Workflow",
                Description = "This is a new workflow",
            };

            // Act
            var result = await _workflowService.CreateWorkflowAsync(workflow);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("New Workflow", result.Name);
            Assert.Equal("This is a new workflow", result.Description);
            Assert.True(result.CreatedAt != default);
            Assert.True(result.UpdatedAt != default);
        }

        [Fact]
        public async Task GetAllWorkflowsAsync_ShouldReturnAllWorkflows()
        {
            // Arrange
            var workflow1 = new Workflow
            {
                Name = "Workflow 1",
                Description = "This is the first workflow",
            };
            var workflow2 = new Workflow
            {
                Name = "Workflow 2",
                Description = "This is the second workflow",
            };
            await _context.Workflows.AddAsync(workflow1);
            await _context.Workflows.AddAsync(workflow2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _workflowService.GetAllWorkflowsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.ToList().Count);  // Convert result to List first
        }

        [Fact]
        public async Task GetWorkflowByIdAsync_ShouldReturnWorkflow_WhenFound()
        {
            // Arrange
            var workflow = new Workflow
            {
                Name = "Test Workflow",
                Description = "This is a test workflow",
            };
            await _context.Workflows.AddAsync(workflow);
            await _context.SaveChangesAsync();

            // Act
            var result = await _workflowService.GetWorkflowByIdAsync(workflow.WorkflowId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(workflow.WorkflowId, result.WorkflowId);
            Assert.Equal("Test Workflow", result.Name);
        }

        [Fact]
        public async Task GetWorkflowByIdAsync_ShouldThrowException_WhenNotFound()
        {
            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _workflowService.GetWorkflowByIdAsync(999));
            Assert.Equal("Workflow not found.", ex.Message);
        }

        [Fact]
        public async Task AddTaskToWorkflowAsync_ShouldAddTaskToWorkflow()
        {
            // Arrange
            var workflow = new Workflow
            {
                Name = "New Workflow",
                Description = "This is a new workflow",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.Workflows.AddAsync(workflow);
            await _context.SaveChangesAsync();

            var taskItem = new TaskItem
            {
                Title = "Task 1",                // Required Title
                Description = "This is a task",  // Required Description
                Status = "Open",                 // Required Status
                AssigneeId = 1,                  // Required AssigneeId
                WorkflowId = null,               // Initially not part of any workflow
                DueDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.Tasks.AddAsync(taskItem);
            await _context.SaveChangesAsync();

            // Act
            await _workflowService.AddTaskToWorkflowAsync(workflow.WorkflowId, taskItem.TaskItemId);

            // Assert
            var updatedTaskItem = await _context.Tasks.FindAsync(taskItem.TaskItemId);
            Assert.NotNull(updatedTaskItem);
            Assert.Equal(workflow.WorkflowId, updatedTaskItem.WorkflowId);
        }

        [Fact]
        public async Task AddTaskToWorkflowAsync_ShouldThrowException_WhenTaskDoesNotExist()
        {
            // Arrange
            var workflow = new Workflow
            {
                Name = "New Workflow",
                Description = "This is a new workflow",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.Workflows.AddAsync(workflow);
            await _context.SaveChangesAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _workflowService.AddTaskToWorkflowAsync(workflow.WorkflowId, 999)
            );
            Assert.Equal("TaskItem not found.", exception.Message);
        }

        [Fact]
        public async Task AddTaskToWorkflowAsync_ShouldThrowException_WhenTaskIsAlreadyInAnotherWorkflow()
        {
            // Arrange
            var workflow1 = new Workflow
            {
                Name = "Workflow 1",
                Description = "This is the first workflow",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            var workflow2 = new Workflow
            {
                Name = "Workflow 2",
                Description = "This is the second workflow",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.Workflows.AddRangeAsync(workflow1, workflow2);
            await _context.SaveChangesAsync();

            var taskItem = new TaskItem
            {
                Title = "Task 1",                // Required Title
                Description = "This is a task",  // Required Description
                Status = "Open",                 // Required Status
                AssigneeId = 1,                  // Required AssigneeId
                WorkflowId = workflow1.WorkflowId,  // Task is already in workflow1
                DueDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.Tasks.AddAsync(taskItem);
            await _context.SaveChangesAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _workflowService.AddTaskToWorkflowAsync(workflow2.WorkflowId, taskItem.TaskItemId)
            );
            Assert.Equal("Task is already part of another workflow.", exception.Message);
        }

        [Fact]
        public async Task AddTaskToWorkflowAsync_ShouldThrowException_WhenDependentTaskDoesNotExist()
        {
            // Arrange
            var workflow = new Workflow
            {
                Name = "New Workflow",
                Description = "This is a new workflow",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.Workflows.AddAsync(workflow);
            await _context.SaveChangesAsync();

            var taskItem = new TaskItem
            {
                Title = "Task 1",                // Required Title
                Description = "This is a task",  // Required Description
                Status = "Open",                 // Required Status
                AssigneeId = 1,                  // Required AssigneeId
                WorkflowId = null,               // Initially not part of any workflow
                DueDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.Tasks.AddAsync(taskItem);
            await _context.SaveChangesAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _workflowService.AddTaskToWorkflowAsync(workflow.WorkflowId, taskItem.TaskItemId, 999)  // Non-existent dependent task
            );
            Assert.Equal("Dependent task not found.", exception.Message);
        }
        [Fact]
        public async Task AddTaskToWorkflowAsync_ShouldThrowException_WhenCyclicDependencyIsDetected()
        {
            // Arrange
            var workflow = new Workflow
            {
                Name = "New Workflow",
                Description = "This is a new workflow",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.Workflows.AddAsync(workflow);
            await _context.SaveChangesAsync();

            var taskItem1 = new TaskItem
            {
                Title = "Task 1",
                Description = "This is task 1",
                Status = "Open",
                AssigneeId = 1,
                WorkflowId = null,  // Not part of any workflow initially
                DueDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.Tasks.AddAsync(taskItem1);

            var taskItem2 = new TaskItem
            {
                Title = "Task 2",
                Description = "This is task 2",
                Status = "Open",
                AssigneeId = 2,
                WorkflowId = null,
                DueDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.Tasks.AddAsync(taskItem2);
            await _context.SaveChangesAsync();

            // Act: Add task 1 and task 2 to the workflow
            await _workflowService.AddTaskToWorkflowAsync(workflow.WorkflowId, taskItem1.TaskItemId);
            await _workflowService.AddTaskToWorkflowAsync(workflow.WorkflowId, taskItem2.TaskItemId);

            // Now create a cyclic dependency by making task 2 depend on task 1
            await _workflowService.AddTaskToWorkflowAsync(workflow.WorkflowId, taskItem1.TaskItemId, taskItem2.TaskItemId);  // Task 1 depends on Task 2

            // Act & Assert: Try to create a cyclic dependency, which should throw an exception
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _workflowService.AddTaskToWorkflowAsync(workflow.WorkflowId, taskItem2.TaskItemId, taskItem1.TaskItemId)  // Now Task 2 depends on Task 1
            );

            Assert.Equal("Cyclic dependency detected.", exception.Message);
        }

        [Fact]
        public async Task ReplaceTaskInWorkflowAsync_ShouldReplaceTaskInWorkflow()
        {
            // Arrange
            var workflow = new Workflow
            {
                Name = "Test Workflow",
                Description = "Test description for workflow",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.Workflows.AddAsync(workflow);
            await _context.SaveChangesAsync();

            // Create and add the old task
            var oldTask = new TaskItem
            {
                Title = "Old Task",
                Description = "This is the old task",
                Status = "Open",
                AssigneeId = 1,
                WorkflowId = null,  // Not assigned to a workflow yet
                DueDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.Tasks.AddAsync(oldTask);

            // Create the new task
            var newTask = new TaskItem
            {
                Title = "New Task",
                Description = "This is the new task",
                Status = "Open",
                AssigneeId = 2,
                WorkflowId = null,  // Not assigned to a workflow yet
                DueDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.Tasks.AddAsync(newTask);

            await _context.SaveChangesAsync();

            // Act: Add the old task to the workflow
            await _workflowService.AddTaskToWorkflowAsync(workflow.WorkflowId, oldTask.TaskItemId);

            // Act: Replace the old task with the new task in the workflow
            await _workflowService.ReplaceTaskInWorkflowAsync(workflow.WorkflowId, oldTask.TaskItemId, newTask.TaskItemId);

            // Assert: Ensure that the old task is no longer in the workflow
            var updatedOldTask = await _context.Tasks.FindAsync(oldTask.TaskItemId);
            if (updatedOldTask == null)
            {
                throw new InvalidOperationException("Old task not found.");
            }

            // Assert: Ensure that the old task is no longer part of the workflow
            Assert.Null(updatedOldTask.WorkflowId);

            // Assert: Ensure that the new task is now part of the workflow
            var updatedNewTask = await _context.Tasks.FindAsync(newTask.TaskItemId);
            if (updatedNewTask == null)
            {
                throw new InvalidOperationException("New task not found.");
            }

            Assert.Equal(workflow.WorkflowId, updatedNewTask.WorkflowId);  // New task should be part of the workflow
        }


        [Fact]
        public async Task ReplaceTaskInWorkflowAsync_ShouldThrowException_WhenOldTaskNotFound()
        {
            // Arrange
            var workflow = new Workflow
            {
                Name = "Test Workflow",
                Description = "Test description for workflow",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.Workflows.AddAsync(workflow);
            await _context.SaveChangesAsync();

            // Create the new task
            var newTask = new TaskItem
            {
                Title = "New Task",
                Description = "This is the new task",
                Status = "Open",
                AssigneeId = 2,
                WorkflowId = null,  // Not assigned to a workflow yet
                DueDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.Tasks.AddAsync(newTask);
            await _context.SaveChangesAsync();

            // Act & Assert: Try to replace a task that doesn't exist
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _workflowService.ReplaceTaskInWorkflowAsync(workflow.WorkflowId, 999, newTask.TaskItemId)  // Non-existent old task
            );
            Assert.Equal("One or both tasks not found.", exception.Message);
        }

        [Fact]
        public async Task ReplaceTaskInWorkflowAsync_ShouldThrowException_WhenOldTaskIsNotPartOfWorkflow()
        {
            // Arrange
            var workflow = new Workflow
            {
                Name = "Test Workflow",
                Description = "Test description for workflow",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.Workflows.AddAsync(workflow);
            await _context.SaveChangesAsync();

            // Create the old task (not yet part of the workflow)
            var oldTask = new TaskItem
            {
                Title = "Old Task",
                Description = "This is the old task",
                Status = "Open",
                AssigneeId = 1,
                WorkflowId = null,  // Not part of any workflow
                DueDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.Tasks.AddAsync(oldTask);

            // Create the new task
            var newTask = new TaskItem
            {
                Title = "New Task",
                Description = "This is the new task",
                Status = "Open",
                AssigneeId = 2,
                WorkflowId = null,  // Not assigned to a workflow yet
                DueDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.Tasks.AddAsync(newTask);
            await _context.SaveChangesAsync();

            // Act & Assert: Try to replace a task that is not part of the workflow
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _workflowService.ReplaceTaskInWorkflowAsync(workflow.WorkflowId, oldTask.TaskItemId, newTask.TaskItemId)
            );
            Assert.Equal("Old task is not part of the workflow.", exception.Message);
        }

        [Fact]
        public async Task ReplaceTaskInWorkflowAsync_ShouldThrowException_WhenNewTaskIsNull()
        {
            // Arrange
            var workflow = new Workflow
            {
                Name = "Test Workflow",
                Description = "Test description for workflow",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.Workflows.AddAsync(workflow);
            await _context.SaveChangesAsync();

            var oldTask = new TaskItem
            {
                Title = "Old Task",
                Description = "This is the old task",
                Status = "Open",
                AssigneeId = 1,
                WorkflowId = null, // Task not assigned to any workflow
                DueDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.Tasks.AddAsync(oldTask);

            // In this case, let's explicitly set newTask to null to simulate the error case
            TaskItem ? newTask = null; // Here, we are simulating the condition where newTask is null

            await _context.SaveChangesAsync();

            // Act & Assert: Try to replace with a null task
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _workflowService.ReplaceTaskInWorkflowAsync(workflow.WorkflowId, oldTask.TaskItemId, newTask?.TaskItemId ?? 0) // Handling null case safely
            );

            Assert.Equal("One or both tasks not found.", exception.Message);
        }


        [Fact]
        public async Task ReassignDependentTasksAsync_ShouldRemoveTasksNotInDependencyOrder()
        {
            // Arrange: Create a workflow with all required fields
            var workflow = new Workflow
            {
                Name = "Test Workflow",
                Description = "Test description for workflow",  // Add a valid Description
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.Workflows.AddAsync(workflow);
            await _context.SaveChangesAsync();  // Save workflow

            // Create tasks that are part of the workflow
            var task1 = new TaskItem
            {
                Title = "Task 1",
                Description = "This is task 1",
                Status = "Open",
                AssigneeId = 1,
                WorkflowId = workflow.WorkflowId,
                DueDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            var task2 = new TaskItem
            {
                Title = "Task 2",
                Description = "This is task 2",
                Status = "Open",
                AssigneeId = 2,
                WorkflowId = workflow.WorkflowId,
                DueDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            var task3 = new TaskItem
            {
                Title = "Task 3",
                Description = "This is task 3",
                Status = "Open",
                AssigneeId = 3,
                WorkflowId = workflow.WorkflowId,
                DueDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.Tasks.AddRangeAsync(task1, task2, task3);
            await _context.SaveChangesAsync(); // Save tasks

            // Set dependencies for tasks
            await _context.TaskDependencies.AddAsync(new TaskDependency
            {
                TaskItemId = task1.TaskItemId,
                DependentTaskItemId = task2.TaskItemId
            });
            await _context.TaskDependencies.AddAsync(new TaskDependency
            {
                TaskItemId = task2.TaskItemId,
                DependentTaskItemId = task3.TaskItemId
            });
            await _context.SaveChangesAsync(); // Save dependencies

            // Define the new dependency order (task1, task3), which will remove task2
            var newDependencyOrder = new List<int> { task1.TaskItemId, task3.TaskItemId };

            // Act: Call the method to reassign dependencies
            await _workflowService.ReassignDependentTasksAsync(workflow.WorkflowId, task1.TaskItemId, newDependencyOrder);

            // Assert: Ensure that task2 is removed from the workflow
            var task2InDb = await _context.Tasks.FindAsync(task2.TaskItemId);

            // Check if task2InDb is not null before trying to access its WorkflowId
            Assert.NotNull(task2InDb); // Ensure that task2 is found in the database
            Assert.Null(task2InDb.WorkflowId);  // Ensure task2 is not part of the workflow anymore
        }

        [Fact]
        public async Task ReassignDependentTasksAsync_ShouldUpdateTaskDependencies()
        {
            // Arrange: Create a workflow and tasks
            var workflow = new Workflow
            {
                Name = "Test Workflow",
                Description = "Test description for workflow",  // Set Description here
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.Workflows.AddAsync(workflow);
            await _context.SaveChangesAsync();

            var task1 = new TaskItem
            {
                TaskItemId = 1,
                Title = "Task 1",
                Description = "Task 1 Description",  // Ensure Description is set
                Status = "Open",
                AssigneeId = 1,
                WorkflowId = workflow.WorkflowId,
                DueDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var task2 = new TaskItem
            {
                TaskItemId = 2,
                Title = "Task 2",
                Description = "Task 2 Description",  // Ensure Description is set
                Status = "Open",
                AssigneeId = 2,
                WorkflowId = workflow.WorkflowId,
                DueDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var task3 = new TaskItem
            {
                TaskItemId = 3,
                Title = "Task 3",
                Description = "Task 3 Description",  // Ensure Description is set
                Status = "Open",
                AssigneeId = 3,
                WorkflowId = workflow.WorkflowId,
                DueDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Tasks.AddRangeAsync(task1, task2, task3);
            await _context.SaveChangesAsync();

            // Set up dependencies
            var taskDependency1 = new TaskDependency { TaskItemId = task1.TaskItemId, DependentTaskItemId = task2.TaskItemId };
            var taskDependency2 = new TaskDependency { TaskItemId = task2.TaskItemId, DependentTaskItemId = task3.TaskItemId };

            await _context.TaskDependencies.AddRangeAsync(taskDependency1, taskDependency2);
            await _context.SaveChangesAsync();

            // Reassign task order: switch task 2 and task 3
            var newDependencyOrder = new List<int> { task1.TaskItemId, task3.TaskItemId, task2.TaskItemId };

            // Act: Reassign task dependencies
            await _workflowService.ReassignDependentTasksAsync(workflow.WorkflowId, task1.TaskItemId, newDependencyOrder);

            // Assert: Ensure that the task dependencies are updated correctly
            var updatedDependencies = await _context.TaskDependencies.ToListAsync();

            // Check the updated task dependencies after reordering
            Assert.Contains(updatedDependencies, td => td.TaskItemId == task1.TaskItemId && td.DependentTaskItemId == task3.TaskItemId);
            Assert.Contains(updatedDependencies, td => td.TaskItemId == task3.TaskItemId && td.DependentTaskItemId == task2.TaskItemId);
        }


        [Fact]
        public async Task ReassignDependentTasksAsync_ShouldThrowException_WhenTaskNotFoundInWorkflow()
        {
            // Arrange: Set up the workflow
            var workflow = new Workflow
            {
                Name = "Test Workflow",
                Description = "Test description for workflow",  // Set Description here to avoid the CS9035 error
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.Workflows.AddAsync(workflow);
            await _context.SaveChangesAsync(); // Save the workflow

            var nonExistentTaskId = 999;  // This task doesn't exist in the workflow
            var newDependencyOrder = new List<int> { nonExistentTaskId };  // New dependency order with a non-existent task

            // Act & Assert: Ensure an exception is thrown when the task isn't found
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _workflowService.ReassignDependentTasksAsync(workflow.WorkflowId, nonExistentTaskId, newDependencyOrder)
            );

            Assert.Equal("Task not found in this workflow.", exception.Message);  // Assert the correct exception message
        }

        // Dispose method to clean up the in-memory database after each test
        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
