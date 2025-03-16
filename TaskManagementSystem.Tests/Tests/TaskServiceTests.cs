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
    public class TaskServiceTests
    {
        private readonly Mock<ILogger<TaskService>> _mockLogger;
        private readonly TaskManagementContext _context;
        private readonly TaskService _taskService;

        public TaskServiceTests()
        {
            _mockLogger = new Mock<ILogger<TaskService>>();

            // Set up in-memory database for testing
            var options = new DbContextOptionsBuilder<TaskManagementContext>()
                            .UseInMemoryDatabase(databaseName: "TestDatabase")
                            .Options;
            _context = new TaskManagementContext(options);

            // Create the TaskService with the in-memory context and logger
            _taskService = new TaskService(_mockLogger.Object, _context);

            // Clear the database at the start of each test
            _context.Database.EnsureDeleted();   // Delete the database if it exists
            _context.Database.EnsureCreated();   // Create a new clean database
        }

        [Fact]
        public async Task CreateTaskAsync_ShouldCreateTask_WhenValidTask()
        {
            // Arrange: Create a valid TaskItem
            var taskItem = new TaskItem
            {
                Title = "Test Task",
                Description = "This is a test task",
                Status = "Open",  // Setting Status
                AssigneeId = 1,
                WorkflowId = 1  // Provide a WorkflowId, which may or may not exist
            };

            // Create a mock user for the assignee (with required Email and PasswordHash)
            var user = new User
            {
                UserId = 1,
                Username = "testuser",
                Role = "Admin",
                Email = "testuser@example.com",  // Set the required Email property
                PasswordHash = "hashedPassword123"  // Set the required PasswordHash property
            };

            // Create and add a Workflow that will be used for the task
            var workflow = new Workflow
            {
                WorkflowId = 1,  // Ensure unique WorkflowId
                Name = "Test Workflow",
                Description = "This is a description for the workflow"  // Set the required Description
            };

            // Add the user and workflow to the context (in-memory database)
            await _context.Users.AddAsync(user);
            await _context.Workflows.AddAsync(workflow);
            await _context.SaveChangesAsync();  // Ensure the user and workflow are saved before testing

            // Act: Call the service to create the task
            var result = await _taskService.CreateTaskAsync(taskItem);

            // Assert: Verify that the task was created correctly
            Assert.NotNull(result);
            Assert.Equal(taskItem.Title, result.Title);
            Assert.Equal(taskItem.Description, result.Description);
            Assert.Equal(taskItem.Status, result.Status);  // Verifying Status
            Assert.Equal(taskItem.AssigneeId, result.AssigneeId);
            Assert.Equal(user, result.Assignee);
            Assert.Equal(workflow, result.Workflow);  // Verifying the Workflow was set correctly
        }

        [Fact]
        public async Task CreateTaskAsync_ShouldThrowException_WhenTaskTitleIsEmpty()
        {
            // Arrange: Create an invalid TaskItem with no title
            var taskItem = new TaskItem
            {
                Title = "",
                Description = "This is a test task",
                Status = "Open",
                AssigneeId = 1
            };

            // Act & Assert: Verify that the exception is thrown for invalid title
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await _taskService.CreateTaskAsync(taskItem));
        }

        [Fact]
        public async Task CreateTaskAsync_ShouldThrowException_WhenAssigneeNotFound()
        {
            // Arrange: Create a task with an invalid AssigneeId
            var taskItem = new TaskItem
            {
                Title = "Test Task",
                Description = "This is a test task",
                Status = "Open",
                AssigneeId = 999  // Invalid AssigneeId
            };

            // Act & Assert: Verify that the exception is thrown for invalid AssigneeId
            await Assert.ThrowsAsync<KeyNotFoundException>(async () => await _taskService.CreateTaskAsync(taskItem));
        }

        [Fact]
        public async Task CreateTaskAsync_ShouldThrowException_WhenWorkflowNotFound()
        {
            // Arrange: Create a task with a non-existent WorkflowId
            var taskItem = new TaskItem
            {
                Title = "Test Task",
                Description = "This is a test task",
                Status = "Open",
                AssigneeId = 1,
                WorkflowId = 999  // Non-existent WorkflowId
            };

            // Create a mock user for the assignee (with required Email and PasswordHash)
            var user = new User
            {
                UserId = 1,
                Username = "testuser",
                Role = "Admin",
                Email = "testuser@example.com",  // Set the required Email property
                PasswordHash = "hashedPassword123"  // Set the required PasswordHash property
            };

            // Add the user to the context (in-memory database)
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();  // Ensure the user is saved before testing

            // Act & Assert: Verify that the exception is thrown for non-existent WorkflowId
            await Assert.ThrowsAsync<KeyNotFoundException>(async () => await _taskService.CreateTaskAsync(taskItem));
        }

        [Fact]
        public async Task UpdateTaskAsync_ShouldUpdateTask_WhenValidTask()
        {
            // Arrange: Create and add a mock user for the assignee
            var user = new User
            {
                UserId = 1,
                Username = "testuser",
                Role = "Admin",
                Email = "testuser@example.com",  // Required Email property
                PasswordHash = "hashedPassword123"  // Required PasswordHash property
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Create a valid TaskItem and add it to the database
            var taskItem = new TaskItem
            {
                Title = "Test Task",
                Description = "This is a test task",
                Status = "Open",
                AssigneeId = 1
            };
            await _context.Tasks.AddAsync(taskItem);
            await _context.SaveChangesAsync();

            // Act: Prepare an update task item
            var updatedTask = new TaskItem
            {
                TaskItemId = taskItem.TaskItemId,
                Title = "Updated Task Title",
                Description = "Updated Description",
                Status = "In Progress",
                AssigneeId = 1,
                WorkflowId = null  // No workflow change
            };

            // Act: Call the service to update the task
            var result = await _taskService.UpdateTaskAsync(taskItem.TaskItemId, updatedTask, 1, true);

            // Assert: Verify that the task was updated correctly
            Assert.NotNull(result);
            Assert.Equal(updatedTask.Title, result.Title);
            Assert.Equal(updatedTask.Description, result.Description);
            Assert.Equal(updatedTask.Status, result.Status);
            Assert.Equal(updatedTask.AssigneeId, result.AssigneeId);
        }

        [Fact]
        public async Task UpdateTaskAsync_ShouldThrowException_WhenTaskNotFound()
        {
            // Arrange: Task ID doesn't exist
            var updatedTask = new TaskItem
            {
                TaskItemId = 999,  // Non-existing Task ID
                Title = "Updated Task",
                Description = "Updated Description",
                Status = "In Progress",
                AssigneeId = 1
            };

            // Act & Assert: Ensure that null is returned (Task not found)
            var result = await _taskService.UpdateTaskAsync(999, updatedTask, 1, true);
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateTaskAsync_ShouldThrowException_WhenWorkflowNotFound()
        {
            // Arrange: Create and add a mock user for the assignee
            var user = new User
            {
                UserId = 1,
                Username = "testuser",
                Role = "Admin",  // Admin role to allow updating WorkflowId
                Email = "testuser@example.com",  // Required Email property
                PasswordHash = "hashedPassword123"  // Required PasswordHash property
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Create and add a task
            var taskItem = new TaskItem
            {
                Title = "Test Task",
                Description = "This is a test task",
                Status = "Open",
                AssigneeId = 1
            };
            await _context.Tasks.AddAsync(taskItem);
            await _context.SaveChangesAsync();

            // Create a TaskItem with an invalid WorkflowId (non-existing Workflow)
            var updatedTask = new TaskItem
            {
                TaskItemId = taskItem.TaskItemId,
                Title = "Updated Task Title",
                Description = "Updated Description",
                Status = "In Progress",
                AssigneeId = 1,
                WorkflowId = 999  // Invalid WorkflowId (assuming no Workflow with ID 999 exists)
            };

            // Act & Assert: Ensure that the exception is thrown when an invalid WorkflowId is provided
            var ex = await Assert.ThrowsAsync<ArgumentException>(async () => await _taskService.UpdateTaskAsync(taskItem.TaskItemId, updatedTask, 1, true));
            Assert.Equal("Invalid Workflow ID.", ex.Message);
        }


        [Fact]
        public async Task UpdateTaskAsync_ShouldThrowException_WhenTaskHasDependencyAndStatusCannotBeUpdated()
        {
            // Arrange: Create and add a mock user for the assignee
            var user = new User
            {
                UserId = 1,
                Username = "testuser",
                Role = "Admin",
                Email = "testuser@example.com",  // Required Email property
                PasswordHash = "hashedPassword123"  // Required PasswordHash property
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Create a task that has a dependent task
            var parentTask = new TaskItem
            {
                Title = "Parent Task",
                Description = "This is the parent task",
                Status = "Open",  // Not completed
                AssigneeId = 1
            };
            var dependentTask = new TaskItem
            {
                Title = "Dependent Task",
                Description = "This is the dependent task",
                Status = "In Progress",  // Trying to change status to In Progress while parent is not completed
                AssigneeId = 1
            };

            await _context.Tasks.AddAsync(parentTask);
            await _context.Tasks.AddAsync(dependentTask);
            await _context.SaveChangesAsync();

            // Create a task dependency
            var dependency = new TaskDependency
            {
                TaskItemId = dependentTask.TaskItemId,
                DependentTaskItemId = parentTask.TaskItemId
            };

            await _context.TaskDependencies.AddAsync(dependency);
            await _context.SaveChangesAsync();

            // Act: Try to update dependent task's status
            var updatedTask = new TaskItem
            {
                TaskItemId = dependentTask.TaskItemId,
                Title = "Updated Dependent Task",
                Description = "Updated Description",
                Status = "Completed",  // Invalid status change
                AssigneeId = 1
            };

            // Act & Assert: Ensure that the exception is thrown for invalid status update due to dependency
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await _taskService.UpdateTaskAsync(dependentTask.TaskItemId, updatedTask, 1, true));
            Assert.Equal("Cannot update task status because dependent tasks are not completed.", ex.Message);
        }
        [Fact]
        public async Task UpdateTaskAsync_ShouldAllowAdminToUpdateWorkflowId()
        {
            // Arrange: Create admin user and task
            var adminUser = new User
            {
                UserId = 1,
                Username = "adminuser",
                Role = "Admin",
                Email = "adminuser@example.com",
                PasswordHash = "hashedPassword123"
            };
            await _context.Users.AddAsync(adminUser);

            var taskItem = new TaskItem
            {
                Title = "Test Task",                    // Required member (Title)
                Description = "This is a test task",    // Required member (Description)
                Status = "Open",                        // Required member (Status)
                AssigneeId = 1,                         // Required member (AssigneeId)
                WorkflowId = null,                      // Optional member (WorkflowId)
                DueDate = DateTime.UtcNow,              // Example of setting the DueDate
                CreatedAt = DateTime.UtcNow,            // Set CreatedAt
                UpdatedAt = DateTime.UtcNow             // Set UpdatedAt
            };
            await _context.Tasks.AddAsync(taskItem);
            await _context.SaveChangesAsync();  // Save task and user

            // Create a valid WorkflowId to update to (assuming a valid Workflow exists with ID 2)
            var workflow = new Workflow
            {
                WorkflowId = 2,
                Name = "Test Workflow",
                Description = "This is a test workflow"  // Ensure you set the required Description property
            };
            await _context.Workflows.AddAsync(workflow);
            await _context.SaveChangesAsync();

            // Act: Admin updates the WorkflowId
            var result = await _taskService.UpdateTaskAsync(taskItem.TaskItemId, new TaskItem
            {
                Title = taskItem.Title,          // Required member (Title)
                Description = taskItem.Description, // Required member (Description)
                Status = taskItem.Status,        // Required member (Status)
                AssigneeId = taskItem.AssigneeId, // Required member (AssigneeId)
                WorkflowId = 2                   // Update WorkflowId
            }, 1, true);  // Admin userId is 1, and isAdmin is true

            // Assert: The task's workflow is updated successfully
            Assert.NotNull(result);
            Assert.Equal(2, result.WorkflowId);
        }



        [Fact]
        public async Task UpdateTaskAsync_ShouldPreventNonAdminFromUpdatingWorkflowId()
        {
            // Arrange: Create non-admin user and task
            var nonAdminUser = new User
            {
                UserId = 2,
                Username = "user1",
                Role = "User",
                Email = "user1@example.com",
                PasswordHash = "hashedPassword123"
            };
            await _context.Users.AddAsync(nonAdminUser);

            var taskItem = new TaskItem
            {
                Title = "Test Task",                    // Required member (Title)
                Description = "This is a test task",    // Required member (Description)
                Status = "Open",                        // Required member (Status)
                AssigneeId = 2,                         // Required member (AssigneeId)
                WorkflowId = null,                      // Optional member (WorkflowId)
                DueDate = DateTime.UtcNow,              // Example of setting the DueDate
                CreatedAt = DateTime.UtcNow,            // Set CreatedAt
                UpdatedAt = DateTime.UtcNow             // Set UpdatedAt
            };
            await _context.Tasks.AddAsync(taskItem);
            await _context.SaveChangesAsync();  // Save task and user

            // Act: Non-admin tries to update the WorkflowId
            var result = await _taskService.UpdateTaskAsync(taskItem.TaskItemId, new TaskItem
            {
                Title = taskItem.Title,          // Required member (Title)
                Description = taskItem.Description, // Required member (Description)
                Status = taskItem.Status,        // Required member (Status)
                AssigneeId = taskItem.AssigneeId, // Required member (AssigneeId)
                WorkflowId = 3                   // Attempting to update WorkflowId
            }, 2, false);

            // Assert: The update is not allowed, task workflow remains unchanged
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateTaskAsync_ShouldAllowAdminToChangeAssignee()
        {
            // Arrange: Create admin user, regular user, and task
            var adminUser = new User
            {
                UserId = 1,
                Username = "adminuser",
                Role = "Admin",
                Email = "adminuser@example.com",
                PasswordHash = "hashedPassword123"
            };

            var regularUser = new User
            {
                UserId = 2,
                Username = "regularuser",
                Role = "User",
                Email = "regularuser@example.com",
                PasswordHash = "hashedPassword123"
            };

            await _context.Users.AddAsync(adminUser);
            await _context.Users.AddAsync(regularUser);
            await _context.SaveChangesAsync();

            var taskItem = new TaskItem
            {
                Title = "Test Task",
                Description = "This is a test task",
                Status = "Open",
                AssigneeId = 2,  // Regular user is the initial assignee
                WorkflowId = null,
                DueDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Tasks.AddAsync(taskItem);
            await _context.SaveChangesAsync();

            // Act: Admin updates the assignee to a new user
            var result = await _taskService.UpdateTaskAsync(taskItem.TaskItemId, new TaskItem
            {
                Title = taskItem.Title,
                Description = taskItem.Description,
                Status = taskItem.Status,
                AssigneeId = 1,  // Admin is the new assignee
                WorkflowId = taskItem.WorkflowId
            }, 1, true);  // Admin userId is 1, and isAdmin is true

            // Assert: The task's assignee is updated successfully by the admin
            Assert.NotNull(result);
            Assert.Equal(1, result.AssigneeId);  // Admin should be assigned to the task
        }

        [Fact]
        public async Task UpdateTaskAsync_ShouldAllowCurrentAssigneeToChangeAssignee()
        {
            // Arrange: Create regular user (current assignee) and another user to be the new assignee
            var regularUser = new User
            {
                UserId = 1,
                Username = "regularuser",
                Role = "User",
                Email = "regularuser@example.com",
                PasswordHash = "hashedPassword123"
            };

            var newAssignee = new User
            {
                UserId = 2,
                Username = "newassignee",
                Role = "User",
                Email = "newassignee@example.com",
                PasswordHash = "hashedPassword123"
            };

            await _context.Users.AddAsync(regularUser);
            await _context.Users.AddAsync(newAssignee);
            await _context.SaveChangesAsync();

            var taskItem = new TaskItem
            {
                Title = "Test Task",
                Description = "This is a test task",
                Status = "Open",
                AssigneeId = 1,  // Regular user is the initial assignee
                WorkflowId = null,
                DueDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Tasks.AddAsync(taskItem);
            await _context.SaveChangesAsync();

            // Act: Current assignee (regular user) updates the assignee to the new assignee
            var result = await _taskService.UpdateTaskAsync(taskItem.TaskItemId, new TaskItem
            {
                Title = taskItem.Title,
                Description = taskItem.Description,
                Status = taskItem.Status,
                AssigneeId = 2,  // New user is the assignee
                WorkflowId = taskItem.WorkflowId
            }, 1, false);  // Regular user userId is 1, and isAdmin is false

            // Assert: The task's assignee is updated successfully by the current assignee
            Assert.NotNull(result);
            Assert.Equal(2, result.AssigneeId);  // New assignee should be assigned to the task
        }

        [Fact]
        public async Task UpdateTaskAsync_ShouldNotAllowNonAssigneeToChangeAssignee()
        {
            // Arrange: Create users and task
            var adminUser = new User
            {
                UserId = 1,
                Username = "adminuser",
                Role = "Admin",
                Email = "adminuser@example.com",
                PasswordHash = "hashedPassword123"
            };

            var regularUser = new User
            {
                UserId = 2,
                Username = "regularuser",
                Role = "User",
                Email = "regularuser@example.com",
                PasswordHash = "hashedPassword123"
            };

            var nonAssignee = new User
            {
                UserId = 3,
                Username = "nonassignee",
                Role = "User",
                Email = "nonassignee@example.com",
                PasswordHash = "hashedPassword123"
            };

            await _context.Users.AddAsync(adminUser);
            await _context.Users.AddAsync(regularUser);
            await _context.Users.AddAsync(nonAssignee);
            await _context.SaveChangesAsync();

            var taskItem = new TaskItem
            {
                Title = "Test Task",
                Description = "This is a test task",
                Status = "Open",
                AssigneeId = 2,  // Regular user is the initial assignee
                WorkflowId = null,
                DueDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Tasks.AddAsync(taskItem);
            await _context.SaveChangesAsync();

            // Act: Non-assigned user tries to change the assignee (should not be allowed)
            var result = await _taskService.UpdateTaskAsync(taskItem.TaskItemId, new TaskItem
            {
                Title = taskItem.Title,
                Description = taskItem.Description,
                Status = taskItem.Status,
                AssigneeId = 1,  // Admin user is assigned
                WorkflowId = taskItem.WorkflowId
            }, 3, false);  // Non-assigned user userId is 3, and isAdmin is false

            // Assert: The task's assignee was not updated (should return null)
            Assert.Null(result);
        }
        [Fact]
        public async Task DeleteTaskAsync_ShouldDeleteTask_WhenNoDependencies()
        {
            // Arrange: Create a task and add it to the context
            var taskItem = new TaskItem
            {
                Title = "Test Task",
                Description = "This is a test task",
                Status = "Open",
                AssigneeId = 1,
                WorkflowId = null,  // No WorkflowId (not part of a workflow)
                DueDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.Tasks.AddAsync(taskItem);
            await _context.SaveChangesAsync();

            // Act: Delete the task
            var result = await _taskService.DeleteTaskAsync(taskItem.TaskItemId);

            // Assert: The task is deleted successfully
            Assert.True(result);
            Assert.Null(await _context.Tasks.FindAsync(taskItem.TaskItemId));  // Task should no longer exist in the database
        }
        [Fact]
        public async Task GetAllTasksAsync_ShouldReturnAllTasks_WhenTasksExist()
        {
            // Arrange: Create a user and task with workflow
            var user = new User
            {
                UserId = 1,
                Username = "user1",
                Role = "Admin",
                Email = "user1@example.com",
                PasswordHash = "hashedPassword123"
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var workflow = new Workflow
            {
                WorkflowId = 1,
                Name = "Test Workflow",
                Description = "Test Description"
            };
            await _context.Workflows.AddAsync(workflow);
            await _context.SaveChangesAsync();

            var taskItem = new TaskItem
            {
                Title = "Test Task",
                Description = "This is a test task",
                Status = "Open",
                AssigneeId = user.UserId,
                WorkflowId = workflow.WorkflowId,
                DueDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.Tasks.AddAsync(taskItem);
            await _context.SaveChangesAsync();

            // Act: Call GetAllTasksAsync to retrieve all tasks
            var tasks = await _taskService.GetAllTasksAsync();

            // Assert: The list of tasks should not be empty and should contain the task added
            Assert.NotEmpty(tasks);
            Assert.Contains(tasks, t => t.TaskItemId == taskItem.TaskItemId);
            Assert.Equal("Test Task", tasks.First().Title);
            Assert.Equal("Test Workflow", tasks.First().Workflow?.Name);
            Assert.Equal("user1", tasks.First().Assignee?.Username);
        }

        [Fact]
        public async Task GetAllTasksAsync_ShouldReturnEmptyList_WhenNoTasksExist()
        {
            // Act: Call GetAllTasksAsync when there are no tasks in the database
            var tasks = await _taskService.GetAllTasksAsync();

            // Assert: The returned list should be empty
            Assert.Empty(tasks);
        }
        [Fact]
        public async Task GetTaskByIdAsync_ShouldReturnTask_WhenTaskExists()
        {
            // Arrange: Create a user, workflow, and task
            var user = new User
            {
                UserId = 1,
                Username = "user1",
                Role = "Admin",
                Email = "user1@example.com",
                PasswordHash = "hashedPassword123"
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var workflow = new Workflow
            {
                WorkflowId = 1,
                Name = "Test Workflow",
                Description = "Test Description"
            };
            await _context.Workflows.AddAsync(workflow);
            await _context.SaveChangesAsync();

            var taskItem = new TaskItem
            {
                Title = "Test Task",
                Description = "This is a test task",
                Status = "Open",
                AssigneeId = user.UserId,
                WorkflowId = workflow.WorkflowId,
                DueDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.Tasks.AddAsync(taskItem);
            await _context.SaveChangesAsync();

            // Act: Call GetTaskByIdAsync to retrieve the task
            var task = await _taskService.GetTaskByIdAsync(taskItem.TaskItemId);

            // Assert: The task should not be null and should contain correct details
            Assert.NotNull(task);
            Assert.Equal(taskItem.TaskItemId, task?.TaskItemId);
            Assert.Equal("Test Task", task?.Title);
            Assert.Equal("Test Workflow", task?.Workflow?.Name);
            Assert.Equal("user1", task?.Assignee?.Username);
        }

        [Fact]
        public async Task GetTaskByIdAsync_ShouldReturnNull_WhenTaskNotFound()
        {
            // Act: Call GetTaskByIdAsync for a non-existent task
            var task = await _taskService.GetTaskByIdAsync(999);  // Assuming task with ID 999 does not exist

            // Assert: The returned task should be null
            Assert.Null(task);
        }


    }
}
