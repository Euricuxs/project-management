using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ProjectManagement.Application.Abstractions.Services;
using ProjectManagement.Application.DTOs.Task;
using ProjectManagement.Application.Exceptions;
using ProjectManagement.Domain.Entities;
using ProjectManagement.Domain.Enums;
using ProjectManagement.Infrastructure.Data;
using ProjectManagement.Infrastructure.Services;
using Xunit;

namespace ProjectManagement.UnitTests.Services;

public class TaskServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IBoardService> _boardServiceMock;
    private readonly Mock<IActivityService> _activityServiceMock;
    private readonly Mock<ILogger<TaskService>> _loggerMock;
    private readonly TaskService _taskService;

    private readonly Guid _userId = Guid.NewGuid();

    public TaskServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new ApplicationDbContext(options);
        _boardServiceMock = new Mock<IBoardService>();
        _activityServiceMock = new Mock<IActivityService>();
        _loggerMock = new Mock<ILogger<TaskService>>();

        _taskService = new TaskService(
            _context,
            _boardServiceMock.Object,
            _activityServiceMock.Object,
            _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private async Task<User> CreateTestUserAsync()
    {
        var user = new User
        {
            Id = _userId,
            Email = "test@example.com",
            PasswordHash = "hash",
            FirstName = "Test",
            LastName = "User"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    private async Task<Project> CreateTestProjectAsync()
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Test Project",
            Key = "TEST"
        };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
        return project;
    }

    private async Task<Board> CreateTestBoardAsync(Guid projectId)
    {
        var board = new Board
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Name = "Test Board"
        };
        _context.Boards.Add(board);
        await _context.SaveChangesAsync();
        return board;
    }

    private async Task<Column> CreateTestColumnAsync(Guid boardId, string name = "To Do")
    {
        var column = new Column
        {
            Id = Guid.NewGuid(),
            BoardId = boardId,
            Name = name,
            Position = 0
        };
        _context.Columns.Add(column);
        await _context.SaveChangesAsync();
        return column;
    }

    private async Task<TaskItem> CreateTestTaskAsync(Guid columnId, string title = "Test Task")
    {
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            ColumnId = columnId,
            Title = title,
            TaskKey = "TEST-1",
            ReporterId = _userId
        };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();
        return task;
    }

    #region CreateTask Tests

    [Fact]
    public async Task CreateTaskAsync_WithValidRequest_CreatesTask()
    {
        // Arrange
        await CreateTestUserAsync();
        var project = await CreateTestProjectAsync();
        var board = await CreateTestBoardAsync(project.Id);
        var column = await CreateTestColumnAsync(board.Id);

        _boardServiceMock.Setup(x => x.UserHasAccessAsync(board.Id, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new CreateTaskRequest
        {
            ColumnId = column.Id,
            Title = "New Task"
        };

        // Act
        var result = await _taskService.CreateTaskAsync(request, _userId);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("New Task");
        result.ColumnId.Should().Be(column.Id);
        result.Status.Should().Be("Todo");
        result.Priority.Should().Be("Medium");

        var dbTask = await _context.Tasks.FirstOrDefaultAsync(t => t.Title == "New Task");
        dbTask.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateTaskAsync_GeneratesUniqueTaskKey()
    {
        // Arrange
        await CreateTestUserAsync();
        var project = await CreateTestProjectAsync();
        var board = await CreateTestBoardAsync(project.Id);
        var column = await CreateTestColumnAsync(board.Id);

        _boardServiceMock.Setup(x => x.UserHasAccessAsync(board.Id, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Create first task
        var request1 = new CreateTaskRequest { ColumnId = column.Id, Title = "Task 1" };
        var result1 = await _taskService.CreateTaskAsync(request1, _userId);

        // Create second task
        var request2 = new CreateTaskRequest { ColumnId = column.Id, Title = "Task 2" };
        var result2 = await _taskService.CreateTaskAsync(request2, _userId);

        // Assert
        result1.TaskKey.Should().NotBe(result2.TaskKey);
    }

    [Fact]
    public async Task CreateTaskAsync_WithInvalidColumn_ThrowsNotFoundException()
    {
        // Arrange
        await CreateTestUserAsync();
        var request = new CreateTaskRequest
        {
            ColumnId = Guid.NewGuid(),
            Title = "Orphan Task"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _taskService.CreateTaskAsync(request, _userId));

        exception.Message.Should().Contain("Column not found");
    }

    [Fact]
    public async Task CreateTaskAsync_WithoutBoardAccess_ThrowsForbiddenException()
    {
        // Arrange
        await CreateTestUserAsync();
        var project = await CreateTestProjectAsync();
        var board = await CreateTestBoardAsync(project.Id);
        var column = await CreateTestColumnAsync(board.Id);

        _boardServiceMock.Setup(x => x.UserHasAccessAsync(board.Id, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var request = new CreateTaskRequest
        {
            ColumnId = column.Id,
            Title = "Unauthorized Task"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(
            () => _taskService.CreateTaskAsync(request, _userId));
    }

    [Fact]
    public async Task CreateTaskAsync_WithEmptyTitle_ThrowsValidationException()
    {
        // Arrange
        await CreateTestUserAsync();
        var project = await CreateTestProjectAsync();
        var board = await CreateTestBoardAsync(project.Id);
        var column = await CreateTestColumnAsync(board.Id);

        _boardServiceMock.Setup(x => x.UserHasAccessAsync(board.Id, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new CreateTaskRequest
        {
            ColumnId = column.Id,
            Title = "   " // Whitespace only
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _taskService.CreateTaskAsync(request, _userId));

        exception.Message.Should().Contain("title is required");
    }

    [Fact]
    public async Task CreateTaskAsync_WithInvalidPriority_ThrowsValidationException()
    {
        // Arrange
        await CreateTestUserAsync();
        var project = await CreateTestProjectAsync();
        var board = await CreateTestBoardAsync(project.Id);
        var column = await CreateTestColumnAsync(board.Id);

        _boardServiceMock.Setup(x => x.UserHasAccessAsync(board.Id, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new CreateTaskRequest
        {
            ColumnId = column.Id,
            Title = "Task with invalid priority",
            Priority = "InvalidPriority"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _taskService.CreateTaskAsync(request, _userId));

        exception.Message.Should().Contain("Invalid priority");
    }

    [Fact]
    public async Task CreateTaskAsync_WithInvalidStatus_ThrowsValidationException()
    {
        // Arrange
        await CreateTestUserAsync();
        var project = await CreateTestProjectAsync();
        var board = await CreateTestBoardAsync(project.Id);
        var column = await CreateTestColumnAsync(board.Id);

        _boardServiceMock.Setup(x => x.UserHasAccessAsync(board.Id, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new CreateTaskRequest
        {
            ColumnId = column.Id,
            Title = "Task with invalid status",
            Status = "InvalidStatus"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _taskService.CreateTaskAsync(request, _userId));

        exception.Message.Should().Contain("Invalid status");
    }

    [Fact]
    public async Task CreateTaskAsync_SetsPositionToEnd_WhenPositionNotProvided()
    {
        // Arrange
        await CreateTestUserAsync();
        var project = await CreateTestProjectAsync();
        var board = await CreateTestBoardAsync(project.Id);
        var column = await CreateTestColumnAsync(board.Id);

        _boardServiceMock.Setup(x => x.UserHasAccessAsync(board.Id, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Create tasks without position
        var task1 = await _taskService.CreateTaskAsync(
            new CreateTaskRequest { ColumnId = column.Id, Title = "Task 1" }, _userId);
        var task2 = await _taskService.CreateTaskAsync(
            new CreateTaskRequest { ColumnId = column.Id, Title = "Task 2" }, _userId);
        var task3 = await _taskService.CreateTaskAsync(
            new CreateTaskRequest { ColumnId = column.Id, Title = "Task 3" }, _userId);

        // Assert
        task1.Position.Should().Be(0);
        task2.Position.Should().Be(1);
        task3.Position.Should().Be(2);
    }

    [Fact]
    public async Task CreateTaskAsync_LogsActivity()
    {
        // Arrange
        await CreateTestUserAsync();
        var project = await CreateTestProjectAsync();
        var board = await CreateTestBoardAsync(project.Id);
        var column = await CreateTestColumnAsync(board.Id);

        _boardServiceMock.Setup(x => x.UserHasAccessAsync(board.Id, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new CreateTaskRequest { ColumnId = column.Id, Title = "Activity Test Task" };

        // Act
        await _taskService.CreateTaskAsync(request, _userId);

        // Assert
        _activityServiceMock.Verify(x => x.LogActivityAsync(
            _userId,
            It.IsAny<string?>(),
            "Task",
            It.IsAny<Guid>(),
            "Activity Test Task",
            project.Id,
            "TaskCreated",
            It.IsAny<string>(),
            It.IsAny<object?>(),
            It.IsAny<object?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region UpdateTask Tests

    [Fact]
    public async Task UpdateTaskAsync_WithValidRequest_UpdatesTask()
    {
        // Arrange
        await CreateTestUserAsync();
        var project = await CreateTestProjectAsync();
        var board = await CreateTestBoardAsync(project.Id);
        var column = await CreateTestColumnAsync(board.Id);
        var task = await CreateTestTaskAsync(column.Id, "Original Title");

        _boardServiceMock.Setup(x => x.UserHasAccessAsync(board.Id, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new UpdateTaskRequest { Title = "Updated Title" };

        // Act
        var result = await _taskService.UpdateTaskAsync(task.Id, request, _userId);

        // Assert
        result.Title.Should().Be("Updated Title");
    }

    [Fact]
    public async Task UpdateTaskAsync_ToDoneStatus_SetsCompletedAt()
    {
        // Arrange
        await CreateTestUserAsync();
        var project = await CreateTestProjectAsync();
        var board = await CreateTestBoardAsync(project.Id);
        var column = await CreateTestColumnAsync(board.Id);
        var task = await CreateTestTaskAsync(column.Id);

        _boardServiceMock.Setup(x => x.UserHasAccessAsync(board.Id, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new UpdateTaskRequest { Status = "Done" };

        // Act
        var result = await _taskService.UpdateTaskAsync(task.Id, request, _userId);

        // Assert
        result.CompletedAt.Should().NotBeNull();
        result.Status.Should().Be("Done");
    }

    [Fact]
    public async Task UpdateTaskAsync_FromDoneStatus_ClearsCompletedAt()
    {
        // Arrange
        await CreateTestUserAsync();
        var project = await CreateTestProjectAsync();
        var board = await CreateTestBoardAsync(project.Id);
        var column = await CreateTestColumnAsync(board.Id);
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            ColumnId = column.Id,
            Title = "Done Task",
            TaskKey = "TEST-DONE",
            Status = Domain.Enums.TaskStatus.Done,
            CompletedAt = DateTime.UtcNow.AddDays(-1),
            ReporterId = _userId
        };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        _boardServiceMock.Setup(x => x.UserHasAccessAsync(board.Id, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new UpdateTaskRequest { Status = "InProgress" };

        // Act
        var result = await _taskService.UpdateTaskAsync(task.Id, request, _userId);

        // Assert
        result.CompletedAt.Should().BeNull();
        result.Status.Should().Be("InProgress");
    }

    [Fact]
    public async Task UpdateTaskAsync_AssignUser_LogsTaskAssignedActivity()
    {
        // Arrange
        await CreateTestUserAsync();
        var project = await CreateTestProjectAsync();
        var board = await CreateTestBoardAsync(project.Id);
        var column = await CreateTestColumnAsync(board.Id);
        var task = await CreateTestTaskAsync(column.Id);
        var assigneeId = Guid.NewGuid();

        _boardServiceMock.Setup(x => x.UserHasAccessAsync(board.Id, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new UpdateTaskRequest { AssigneeId = assigneeId };

        // Act
        await _taskService.UpdateTaskAsync(task.Id, request, _userId);

        // Assert
        _activityServiceMock.Verify(x => x.LogActivityAsync(
            _userId,
            It.IsAny<string?>(),
            "Task",
            task.Id,
            "Test Task",
            project.Id,
            "TaskAssigned",
            It.IsAny<string>(),
            It.IsAny<object?>(),
            It.IsAny<object?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTaskAsync_UnassignUser_LogsTaskUnassignedActivity()
    {
        // Arrange
        await CreateTestUserAsync();
        var project = await CreateTestProjectAsync();
        var board = await CreateTestBoardAsync(project.Id);
        var column = await CreateTestColumnAsync(board.Id);
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            ColumnId = column.Id,
            Title = "Assigned Task",
            TaskKey = "TEST-ASSIGNED",
            AssigneeId = Guid.NewGuid(),
            ReporterId = _userId
        };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        _boardServiceMock.Setup(x => x.UserHasAccessAsync(board.Id, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new UpdateTaskRequest { AssigneeId = null };

        // Act
        await _taskService.UpdateTaskAsync(task.Id, request, _userId);

        // Assert
        _activityServiceMock.Verify(x => x.LogActivityAsync(
            _userId,
            It.IsAny<string?>(),
            "Task",
            task.Id,
            "Assigned Task",
            project.Id,
            "TaskUnassigned",
            It.IsAny<string>(),
            It.IsAny<object?>(),
            It.IsAny<object?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTaskAsync_WithEmptyTitle_ThrowsValidationException()
    {
        // Arrange
        await CreateTestUserAsync();
        var project = await CreateTestProjectAsync();
        var board = await CreateTestBoardAsync(project.Id);
        var column = await CreateTestColumnAsync(board.Id);
        var task = await CreateTestTaskAsync(column.Id);

        _boardServiceMock.Setup(x => x.UserHasAccessAsync(board.Id, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new UpdateTaskRequest { Title = "" };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _taskService.UpdateTaskAsync(task.Id, request, _userId));
    }

    [Fact]
    public async Task UpdateTaskAsync_NonExistentTask_ThrowsNotFoundException()
    {
        // Arrange
        var request = new UpdateTaskRequest { Title = "Updated Title" };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _taskService.UpdateTaskAsync(Guid.NewGuid(), request, _userId));
    }

    #endregion

    #region MoveTask Tests

    [Fact]
    public async Task MoveTaskAsync_WithinSameColumn_UpdatesPositions()
    {
        // Arrange
        await CreateTestUserAsync();
        var project = await CreateTestProjectAsync();
        var board = await CreateTestBoardAsync(project.Id);
        var column = await CreateTestColumnAsync(board.Id);

        var task1 = await CreateTestTaskAsync(column.Id, "Task 1");
        var task2 = await CreateTestTaskAsync(column.Id, "Task 2");
        var task3 = await CreateTestTaskAsync(column.Id, "Task 3");

        task1.Position = 0;
        task2.Position = 1;
        task3.Position = 2;
        await _context.SaveChangesAsync();

        _boardServiceMock.Setup(x => x.UserHasAccessAsync(board.Id, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act - Move task1 to position 2
        var result = await _taskService.MoveTaskAsync(task1.Id, column.Id, 2, _userId);

        // Assert
        result.Position.Should().Be(2);
    }

    [Fact]
    public async Task MoveTaskAsync_ToDifferentColumn_MovesTask()
    {
        // Arrange
        await CreateTestUserAsync();
        var project = await CreateTestProjectAsync();
        var board = await CreateTestBoardAsync(project.Id);
        var column1 = await CreateTestColumnAsync(board.Id, "To Do");
        var column2 = await CreateTestColumnAsync(board.Id, "In Progress");
        var task = await CreateTestTaskAsync(column1.Id, "Moving Task");

        _boardServiceMock.Setup(x => x.UserHasAccessAsync(board.Id, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _taskService.MoveTaskAsync(task.Id, column2.Id, 0, _userId);

        // Assert
        result.ColumnId.Should().Be(column2.Id);
        result.Position.Should().Be(0);
    }

    [Fact]
    public async Task MoveTaskAsync_BetweenColumns_LogsTaskMovedActivity()
    {
        // Arrange
        await CreateTestUserAsync();
        var project = await CreateTestProjectAsync();
        var board = await CreateTestBoardAsync(project.Id);
        var column1 = await CreateTestColumnAsync(board.Id, "To Do");
        var column2 = await CreateTestColumnAsync(board.Id, "In Progress");
        var task = await CreateTestTaskAsync(column1.Id, "Moving Task");

        _boardServiceMock.Setup(x => x.UserHasAccessAsync(board.Id, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _taskService.MoveTaskAsync(task.Id, column2.Id, 0, _userId);

        // Assert
        _activityServiceMock.Verify(x => x.LogActivityAsync(
            _userId,
            It.IsAny<string?>(),
            "Task",
            task.Id,
            "Moving Task",
            project.Id,
            "TaskMoved",
            It.IsAny<string>(),
            It.IsAny<object?>(),
            It.IsAny<object?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MoveTaskAsync_ToNonExistentColumn_ThrowsNotFoundException()
    {
        // Arrange
        await CreateTestUserAsync();
        var project = await CreateTestProjectAsync();
        var board = await CreateTestBoardAsync(project.Id);
        var column = await CreateTestColumnAsync(board.Id);
        var task = await CreateTestTaskAsync(column.Id);

        _boardServiceMock.Setup(x => x.UserHasAccessAsync(board.Id, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _taskService.MoveTaskAsync(task.Id, Guid.NewGuid(), 0, _userId));
    }

    [Fact]
    public async Task MoveTaskAsync_NonExistentTask_ThrowsNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _taskService.MoveTaskAsync(Guid.NewGuid(), Guid.NewGuid(), 0, _userId));
    }

    #endregion

    #region DeleteTask Tests

    [Fact]
    public async Task DeleteTaskAsync_PerformsSoftDelete()
    {
        // Arrange
        await CreateTestUserAsync();
        var project = await CreateTestProjectAsync();
        var board = await CreateTestBoardAsync(project.Id);
        var column = await CreateTestColumnAsync(board.Id);
        var task = await CreateTestTaskAsync(column.Id, "Task To Delete");

        _boardServiceMock.Setup(x => x.UserHasAccessAsync(board.Id, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _taskService.DeleteTaskAsync(task.Id, _userId);

        // Assert
        result.Should().BeTrue();

        var dbTask = await _context.Tasks.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == task.Id);
        dbTask!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteTaskAsync_LogsTaskDeletedActivity()
    {
        // Arrange
        await CreateTestUserAsync();
        var project = await CreateTestProjectAsync();
        var board = await CreateTestBoardAsync(project.Id);
        var column = await CreateTestColumnAsync(board.Id);
        var task = await CreateTestTaskAsync(column.Id, "Deleted Task");

        _boardServiceMock.Setup(x => x.UserHasAccessAsync(board.Id, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _taskService.DeleteTaskAsync(task.Id, _userId);

        // Assert
        _activityServiceMock.Verify(x => x.LogActivityAsync(
            _userId,
            It.IsAny<string?>(),
            "Task",
            task.Id,
            "Deleted Task",
            project.Id,
            "TaskDeleted",
            It.IsAny<string>(),
            It.IsAny<object?>(),
            It.IsAny<object?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteTaskAsync_NonExistentTask_ThrowsNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _taskService.DeleteTaskAsync(Guid.NewGuid(), _userId));
    }

    #endregion

    #region GetTasks Tests

    [Fact]
    public async Task GetTasksByColumnAsync_ReturnsOrderedTasks()
    {
        // Arrange
        await CreateTestUserAsync();
        var project = await CreateTestProjectAsync();
        var board = await CreateTestBoardAsync(project.Id);
        var column = await CreateTestColumnAsync(board.Id);

        var task1 = await CreateTestTaskAsync(column.Id, "Task A");
        var task2 = await CreateTestTaskAsync(column.Id, "Task B");
        var task3 = await CreateTestTaskAsync(column.Id, "Task C");

        task1.Position = 2;
        task2.Position = 0;
        task3.Position = 1;
        await _context.SaveChangesAsync();

        _boardServiceMock.Setup(x => x.UserHasAccessAsync(board.Id, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var results = await _taskService.GetTasksByColumnAsync(column.Id, _userId);

        // Assert
        results.Should().HaveCount(3);
        results[0].Title.Should().Be("Task B");
        results[1].Title.Should().Be("Task C");
        results[2].Title.Should().Be("Task A");
    }

    [Fact]
    public async Task GetTasksByColumnAsync_WithoutAccess_ThrowsForbiddenException()
    {
        // Arrange
        await CreateTestUserAsync();
        var project = await CreateTestProjectAsync();
        var board = await CreateTestBoardAsync(project.Id);
        var column = await CreateTestColumnAsync(board.Id);

        _boardServiceMock.Setup(x => x.UserHasAccessAsync(board.Id, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(
            () => _taskService.GetTasksByColumnAsync(column.Id, _userId));
    }

    [Fact]
    public async Task GetTasksByBoardAsync_ReturnsTasksFromAllColumns()
    {
        // Arrange
        await CreateTestUserAsync();
        var project = await CreateTestProjectAsync();
        var board = await CreateTestBoardAsync(project.Id);
        var column1 = await CreateTestColumnAsync(board.Id, "Column 1");
        var column2 = await CreateTestColumnAsync(board.Id, "Column 2");

        await CreateTestTaskAsync(column1.Id, "Task in Column 1");
        await CreateTestTaskAsync(column2.Id, "Task in Column 2");

        _boardServiceMock.Setup(x => x.UserHasAccessAsync(board.Id, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var results = await _taskService.GetTasksByBoardAsync(board.Id, _userId);

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTaskByIdAsync_ReturnsTask()
    {
        // Arrange
        await CreateTestUserAsync();
        var project = await CreateTestProjectAsync();
        var board = await CreateTestBoardAsync(project.Id);
        var column = await CreateTestColumnAsync(board.Id);
        var task = await CreateTestTaskAsync(column.Id, "Specific Task");

        _boardServiceMock.Setup(x => x.UserHasAccessAsync(board.Id, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _taskService.GetTaskByIdAsync(task.Id, _userId);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Specific Task");
    }

    [Fact]
    public async Task GetTaskByIdAsync_NonExistentTask_ReturnsNull()
    {
        // Act
        var result = await _taskService.GetTaskByIdAsync(Guid.NewGuid(), _userId);

        // Assert
        result.Should().BeNull();
    }

    #endregion
}
