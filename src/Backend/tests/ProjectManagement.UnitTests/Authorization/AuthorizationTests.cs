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

namespace ProjectManagement.UnitTests.Authorization;

public class AuthorizationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IActivityService> _activityServiceMock;

    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Guid _adminId = Guid.NewGuid();
    private readonly Guid _memberId = Guid.NewGuid();
    private readonly Guid _guestId = Guid.NewGuid();
    private readonly Guid _outsiderId = Guid.NewGuid();

    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _projectId = Guid.NewGuid();
    private readonly Guid _boardId = Guid.NewGuid();
    private readonly Guid _columnId = Guid.NewGuid();

    public AuthorizationTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new ApplicationDbContext(options);
        _activityServiceMock = new Mock<IActivityService>();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private async Task SetupTestDataAsync()
    {
        // Create users
        var users = new[]
        {
            new User { Id = _ownerId, Email = "owner@test.com", PasswordHash = "hash", FirstName = "Owner", LastName = "User" },
            new User { Id = _adminId, Email = "admin@test.com", PasswordHash = "hash", FirstName = "Admin", LastName = "User" },
            new User { Id = _memberId, Email = "member@test.com", PasswordHash = "hash", FirstName = "Member", LastName = "User" },
            new User { Id = _guestId, Email = "guest@test.com", PasswordHash = "hash", FirstName = "Guest", LastName = "User" },
            new User { Id = _outsiderId, Email = "outsider@test.com", PasswordHash = "hash", FirstName = "Outsider", LastName = "User" }
        };
        _context.Users.AddRange(users);

        // Create workspace with owner
        var workspace = new Workspace
        {
            Id = _workspaceId,
            Name = "Test Workspace",
            OwnerId = _ownerId,
            IsPublic = false
        };
        _context.Workspaces.Add(workspace);

        // Create workspace members with different roles
        var memberships = new[]
        {
            new WorkspaceMember { WorkspaceId = _workspaceId, UserId = _ownerId, Role = WorkspaceRole.Owner },
            new WorkspaceMember { WorkspaceId = _workspaceId, UserId = _adminId, Role = WorkspaceRole.Admin },
            new WorkspaceMember { WorkspaceId = _workspaceId, UserId = _memberId, Role = WorkspaceRole.Member },
            new WorkspaceMember { WorkspaceId = _workspaceId, UserId = _guestId, Role = WorkspaceRole.Guest }
        };
        _context.WorkspaceMembers.AddRange(memberships);

        // Create project
        var project = new Project
        {
            Id = _projectId,
            WorkspaceId = _workspaceId,
            Name = "Test Project",
            Key = "TEST"
        };
        _context.Projects.Add(project);

        // Create board
        var board = new Board
        {
            Id = _boardId,
            ProjectId = _projectId,
            Name = "Test Board"
        };
        _context.Boards.Add(board);

        // Create column
        var column = new Column
        {
            Id = _columnId,
            BoardId = _boardId,
            Name = "To Do",
            Position = 0
        };
        _context.Columns.Add(column);

        await _context.SaveChangesAsync();
    }

    #region Workspace Authorization Tests

    [Fact]
    public async Task Workspace_OwnerCanDelete()
    {
        // Arrange
        await SetupTestDataAsync();
        var service = new WorkspaceService(_context);

        // Act
        var result = await service.DeleteWorkspaceAsync(_workspaceId, _ownerId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task Workspace_AdminCannotDelete()
    {
        // Arrange
        await SetupTestDataAsync();
        var service = new WorkspaceService(_context);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.DeleteWorkspaceAsync(_workspaceId, _adminId));
    }

    [Fact]
    public async Task Workspace_MemberCannotDelete()
    {
        // Arrange
        await SetupTestDataAsync();
        var service = new WorkspaceService(_context);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.DeleteWorkspaceAsync(_workspaceId, _memberId));
    }

    [Fact]
    public async Task Workspace_GuestCannotDelete()
    {
        // Arrange
        await SetupTestDataAsync();
        var service = new WorkspaceService(_context);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.DeleteWorkspaceAsync(_workspaceId, _guestId));
    }

    [Fact]
    public async Task Workspace_OutsiderCannotDelete()
    {
        // Arrange
        await SetupTestDataAsync();
        var service = new WorkspaceService(_context);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.DeleteWorkspaceAsync(_workspaceId, _outsiderId));
    }

    [Fact]
    public async Task Workspace_OwnerCanUpdate()
    {
        // Arrange
        await SetupTestDataAsync();
        var service = new WorkspaceService(_context);

        // Act
        var result = await service.UpdateWorkspaceAsync(
            _workspaceId,
            new Application.DTOs.Workspace.UpdateWorkspaceRequest { Name = "Updated Name" },
            _ownerId);

        // Assert
        result.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task Workspace_AdminCanUpdate()
    {
        // Arrange
        await SetupTestDataAsync();
        var service = new WorkspaceService(_context);

        // Act
        var result = await service.UpdateWorkspaceAsync(
            _workspaceId,
            new Application.DTOs.Workspace.UpdateWorkspaceRequest { Name = "Admin Updated" },
            _adminId);

        // Assert
        result.Name.Should().Be("Admin Updated");
    }

    [Fact]
    public async Task Workspace_MemberCannotUpdate()
    {
        // Arrange
        await SetupTestDataAsync();
        var service = new WorkspaceService(_context);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.UpdateWorkspaceAsync(
                _workspaceId,
                new Application.DTOs.Workspace.UpdateWorkspaceRequest { Name = "Hacked" },
                _memberId));
    }

    [Fact]
    public async Task Workspace_GuestCannotUpdate()
    {
        // Arrange
        await SetupTestDataAsync();
        var service = new WorkspaceService(_context);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.UpdateWorkspaceAsync(
                _workspaceId,
                new Application.DTOs.Workspace.UpdateWorkspaceRequest { Name = "Hacked" },
                _guestId));
    }

    [Fact]
    public async Task Workspace_CannotDeleteLastWorkspace()
    {
        // Arrange
        await SetupTestDataAsync();
        var service = new WorkspaceService(_context);

        // This is the user's only workspace, so deletion should fail with business rule
        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.DeleteWorkspaceAsync(_workspaceId, _ownerId));

        exception.Message.Should().Contain("last workspace");
    }

    #endregion

    #region Workspace Access Tests

    [Fact]
    public async Task Workspace_MemberHasAccess()
    {
        // Arrange
        await SetupTestDataAsync();
        var service = new WorkspaceService(_context);

        // Act
        var result = await service.UserHasAccessAsync(_workspaceId, _memberId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task Workspace_GuestHasAccess()
    {
        // Arrange
        await SetupTestDataAsync();
        var service = new WorkspaceService(_context);

        // Act
        var result = await service.UserHasAccessAsync(_workspaceId, _guestId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task Workspace_OutsiderHasNoAccess()
    {
        // Arrange
        await SetupTestDataAsync();
        var service = new WorkspaceService(_context);

        // Act
        var result = await service.UserHasAccessAsync(_workspaceId, _outsiderId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Workspace_PublicWorkspace_OutsiderHasAccess()
    {
        // Arrange
        await SetupTestDataAsync();
        var workspace = await _context.Workspaces.FindAsync(_workspaceId);
        workspace!.IsPublic = true;
        await _context.SaveChangesAsync();

        var service = new WorkspaceService(_context);

        // Act
        var result = await service.UserHasAccessAsync(_workspaceId, _outsiderId);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Project Authorization Tests

    [Fact]
    public async Task Project_MemberCanCreate()
    {
        // Arrange
        await SetupTestDataAsync();
        var workspaceService = new WorkspaceService(_context);
        var projectService = new ProjectService(_context, workspaceService, _activityServiceMock.Object);

        var request = new Application.DTOs.Project.CreateProjectRequest
        {
            WorkspaceId = _workspaceId,
            Name = "Member Created Project"
        };

        // Act
        var result = await projectService.CreateProjectAsync(request, _memberId);

        // Assert
        result.Name.Should().Be("Member Created Project");
    }

    [Fact]
    public async Task Project_GuestCanCreate()
    {
        // Arrange
        await SetupTestDataAsync();
        var workspaceService = new WorkspaceService(_context);
        var projectService = new ProjectService(_context, workspaceService, _activityServiceMock.Object);

        var request = new Application.DTOs.Project.CreateProjectRequest
        {
            WorkspaceId = _workspaceId,
            Name = "Guest Created Project"
        };

        // Act
        var result = await projectService.CreateProjectAsync(request, _guestId);

        // Assert
        result.Name.Should().Be("Guest Created Project");
    }

    [Fact]
    public async Task Project_OutsiderCannotCreate()
    {
        // Arrange
        await SetupTestDataAsync();
        var workspaceService = new WorkspaceService(_context);
        var projectService = new ProjectService(_context, workspaceService, _activityServiceMock.Object);

        var request = new Application.DTOs.Project.CreateProjectRequest
        {
            WorkspaceId = _workspaceId,
            Name = "Unauthorized Project"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(
            () => projectService.CreateProjectAsync(request, _outsiderId));
    }

    [Fact]
    public async Task Project_MemberCanRead()
    {
        // Arrange
        await SetupTestDataAsync();
        var workspaceService = new WorkspaceService(_context);
        var projectService = new ProjectService(_context, workspaceService, _activityServiceMock.Object);

        // Act
        var result = await projectService.GetProjectByIdAsync(_projectId, _memberId);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Project");
    }

    [Fact]
    public async Task Project_OutsiderCannotRead()
    {
        // Arrange
        await SetupTestDataAsync();
        var workspaceService = new WorkspaceService(_context);
        var projectService = new ProjectService(_context, workspaceService, _activityServiceMock.Object);

        // Act
        var result = await projectService.GetProjectByIdAsync(_projectId, _outsiderId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Board Authorization Tests

    [Fact]
    public async Task Board_MemberCanAccess()
    {
        // Arrange
        await SetupTestDataAsync();
        var workspaceService = new WorkspaceService(_context);
        var projectService = new ProjectService(_context, workspaceService, _activityServiceMock.Object);
        var boardService = new BoardService(_context, projectService);

        // Act
        var result = await boardService.UserHasAccessAsync(_boardId, _memberId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task Board_OutsiderCannotAccess()
    {
        // Arrange
        await SetupTestDataAsync();
        var workspaceService = new WorkspaceService(_context);
        var projectService = new ProjectService(_context, workspaceService, _activityServiceMock.Object);
        var boardService = new BoardService(_context, projectService);

        // Act
        var result = await boardService.UserHasAccessAsync(_boardId, _outsiderId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Task Authorization Tests

    [Fact]
    public async Task Task_MemberCanCreate()
    {
        // Arrange
        await SetupTestDataAsync();
        var workspaceService = new WorkspaceService(_context);
        var projectService = new ProjectService(_context, workspaceService, _activityServiceMock.Object);
        var boardService = new BoardService(_context, projectService);
        var taskService = new TaskService(_context, boardService, _activityServiceMock.Object, NullLogger<TaskService>.Instance);

        var request = new CreateTaskRequest
        {
            ColumnId = _columnId,
            Title = "Member Created Task"
        };

        // Act
        var result = await taskService.CreateTaskAsync(request, _memberId);

        // Assert
        result.Title.Should().Be("Member Created Task");
    }

    [Fact]
    public async Task Task_GuestCanCreate()
    {
        // Arrange
        await SetupTestDataAsync();
        var workspaceService = new WorkspaceService(_context);
        var projectService = new ProjectService(_context, workspaceService, _activityServiceMock.Object);
        var boardService = new BoardService(_context, projectService);
        var taskService = new TaskService(_context, boardService, _activityServiceMock.Object, NullLogger<TaskService>.Instance);

        var request = new CreateTaskRequest
        {
            ColumnId = _columnId,
            Title = "Guest Created Task"
        };

        // Act
        var result = await taskService.CreateTaskAsync(request, _guestId);

        // Assert
        result.Title.Should().Be("Guest Created Task");
    }

    [Fact]
    public async Task Task_OutsiderCannotCreate()
    {
        // Arrange
        await SetupTestDataAsync();
        var workspaceService = new WorkspaceService(_context);
        var projectService = new ProjectService(_context, workspaceService, _activityServiceMock.Object);
        var boardService = new BoardService(_context, projectService);
        var taskService = new TaskService(_context, boardService, _activityServiceMock.Object, NullLogger<TaskService>.Instance);

        var request = new CreateTaskRequest
        {
            ColumnId = _columnId,
            Title = "Unauthorized Task"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(
            () => taskService.CreateTaskAsync(request, _outsiderId));
    }

    [Fact]
    public async Task Task_OutsiderCannotRead()
    {
        // Arrange
        await SetupTestDataAsync();

        // Create a task first
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            ColumnId = _columnId,
            Title = "Test Task",
            TaskKey = "TEST-1",
            ReporterId = _memberId
        };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        var workspaceService = new WorkspaceService(_context);
        var projectService = new ProjectService(_context, workspaceService, _activityServiceMock.Object);
        var boardService = new BoardService(_context, projectService);
        var taskService = new TaskService(_context, boardService, _activityServiceMock.Object, NullLogger<TaskService>.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(
            () => taskService.GetTaskByIdAsync(task.Id, _outsiderId));
    }

    #endregion

    #region Cascade Authorization Tests

    [Fact]
    public async Task Cascade_ProjectAccess_DelegatesToWorkspace()
    {
        // Arrange
        await SetupTestDataAsync();
        var workspaceService = new WorkspaceService(_context);
        var projectService = new ProjectService(_context, workspaceService, _activityServiceMock.Object);

        // Verify outsider has no access
        var hasAccess = await projectService.UserHasAccessAsync(_projectId, _outsiderId);

        // Assert
        hasAccess.Should().BeFalse();
    }

    [Fact]
    public async Task Cascade_BoardAccess_DelegatesToProject()
    {
        // Arrange
        await SetupTestDataAsync();
        var workspaceService = new WorkspaceService(_context);
        var projectService = new ProjectService(_context, workspaceService, _activityServiceMock.Object);
        var boardService = new BoardService(_context, projectService);

        // Verify outsider has no access
        var hasAccess = await boardService.UserHasAccessAsync(_boardId, _outsiderId);

        // Assert
        hasAccess.Should().BeFalse();
    }

    [Fact]
    public async Task Cascade_TaskAccess_DelegatesToBoard()
    {
        // Arrange
        await SetupTestDataAsync();

        // Create a task
        var taskId = Guid.NewGuid();
        var task = new TaskItem
        {
            Id = taskId,
            ColumnId = _columnId,
            Title = "Test Task",
            TaskKey = "TEST-1",
            ReporterId = _memberId
        };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        var workspaceService = new WorkspaceService(_context);
        var projectService = new ProjectService(_context, workspaceService, _activityServiceMock.Object);
        var boardService = new BoardService(_context, projectService);
        var taskService = new TaskService(_context, boardService, _activityServiceMock.Object, NullLogger<TaskService>.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(
            () => taskService.DeleteTaskAsync(taskId, _outsiderId));
    }

    #endregion
}
