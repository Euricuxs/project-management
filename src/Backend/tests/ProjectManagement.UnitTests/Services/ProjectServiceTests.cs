using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using ProjectManagement.Application.Abstractions.Services;
using ProjectManagement.Application.DTOs.Project;
using ProjectManagement.Application.Exceptions;
using ProjectManagement.Domain.Entities;
using ProjectManagement.Domain.Enums;
using ProjectManagement.Infrastructure.Data;
using ProjectManagement.Infrastructure.Services;
using Xunit;

namespace ProjectManagement.UnitTests.Services;

public class ProjectServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IWorkspaceService> _workspaceServiceMock;
    private readonly Mock<IActivityService> _activityServiceMock;
    private readonly ProjectService _projectService;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();

    public ProjectServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _workspaceServiceMock = new Mock<IWorkspaceService>();
        _activityServiceMock = new Mock<IActivityService>();

        _projectService = new ProjectService(
            _context,
            _workspaceServiceMock.Object,
            _activityServiceMock.Object);

        // Default setup: user has workspace access
        _workspaceServiceMock.Setup(x => x.UserHasAccessAsync(_workspaceId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private async Task<Workspace> CreateTestWorkspaceAsync()
    {
        var workspace = new Workspace
        {
            Id = _workspaceId,
            Name = "Test Workspace",
            OwnerId = _userId
        };
        _context.Workspaces.Add(workspace);
        await _context.SaveChangesAsync();
        return workspace;
    }

    private async Task<Project> CreateTestProjectAsync(string name = "Test Project", ProjectStatus status = ProjectStatus.Planning)
    {
        var project = new Project
        {
            WorkspaceId = _workspaceId,
            Name = name,
            Key = "TEST",
            Status = status
        };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
        return project;
    }

    #region CreateProject Tests

    [Fact]
    public async Task CreateProjectAsync_WithValidRequest_CreatesProject()
    {
        // Arrange
        await CreateTestWorkspaceAsync();
        var request = new CreateProjectRequest
        {
            WorkspaceId = _workspaceId,
            Name = "New Project",
            Description = "A new test project"
        };

        // Act
        var result = await _projectService.CreateProjectAsync(request, _userId);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New Project");
        result.Description.Should().Be("A new test project");
        result.Status.Should().Be("Planning");
        result.WorkspaceId.Should().Be(_workspaceId);

        var dbProject = await _context.Projects.FirstOrDefaultAsync(p => p.Name == "New Project");
        dbProject.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateProjectAsync_WithoutWorkspaceAccess_ThrowsForbiddenException()
    {
        // Arrange
        var otherWorkspaceId = Guid.NewGuid();
        _workspaceServiceMock.Setup(x => x.UserHasAccessAsync(otherWorkspaceId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var request = new CreateProjectRequest
        {
            WorkspaceId = otherWorkspaceId,
            Name = "Unauthorized Project"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(
            () => _projectService.CreateProjectAsync(request, _userId));
    }

    [Fact]
    public async Task CreateProjectAsync_WithDuplicateName_ThrowsAlreadyExistsException()
    {
        // Arrange
        await CreateTestWorkspaceAsync();
        await CreateTestProjectAsync("Duplicate Project");

        var request = new CreateProjectRequest
        {
            WorkspaceId = _workspaceId,
            Name = "Duplicate Project"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AlreadyExistsException>(
            () => _projectService.CreateProjectAsync(request, _userId));

        exception.Message.Should().Contain("already exists");
    }

    [Fact]
    public async Task CreateProjectAsync_WithAutoKey_GeneratesKey()
    {
        // Arrange
        await CreateTestWorkspaceAsync();
        var request = new CreateProjectRequest
        {
            WorkspaceId = _workspaceId,
            Name = "AutoKey Project"
        };

        // Act
        var result = await _projectService.CreateProjectAsync(request, _userId);

        // Assert
        result.Key.Should().NotBeNullOrEmpty();
        result.Key.Should().StartWith("AUTO");
    }

    [Fact]
    public async Task CreateProjectAsync_WithProvidedKey_UsesProvidedKey()
    {
        // Arrange
        await CreateTestWorkspaceAsync();
        var request = new CreateProjectRequest
        {
            WorkspaceId = _workspaceId,
            Name = "Custom Key Project",
            Key = "CUSTOM"
        };

        // Act
        var result = await _projectService.CreateProjectAsync(request, _userId);

        // Assert
        result.Key.Should().Be("CUSTOM");
    }

    [Fact]
    public async Task CreateProjectAsync_LogsActivity()
    {
        // Arrange
        await CreateTestWorkspaceAsync();
        var request = new CreateProjectRequest
        {
            WorkspaceId = _workspaceId,
            Name = "Activity Test Project"
        };

        // Act
        await _projectService.CreateProjectAsync(request, _userId);

        // Assert
        _activityServiceMock.Verify(x => x.LogActivityAsync(
            _userId,
            It.IsAny<string?>(),
            "Project",
            It.IsAny<Guid>(),
            "Activity Test Project",
            It.IsAny<Guid>(),
            "ProjectCreated",
            It.IsAny<string>(),
            It.IsAny<object?>(),
            It.IsAny<object?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region UpdateProject Tests

    [Fact]
    public async Task UpdateProjectAsync_WithValidRequest_UpdatesProject()
    {
        // Arrange
        await CreateTestWorkspaceAsync();
        var project = await CreateTestProjectAsync("Original Name");

        var request = new UpdateProjectRequest
        {
            Name = "Updated Name",
            Description = "Updated description",
            Status = "Active"
        };

        // Act
        var result = await _projectService.UpdateProjectAsync(project.Id, request, _userId);

        // Assert
        result.Name.Should().Be("Updated Name");
        result.Description.Should().Be("Updated description");
        result.Status.Should().Be("Active");
    }

    [Fact]
    public async Task UpdateProjectAsync_NonExistentProject_ThrowsNotFoundException()
    {
        // Arrange
        var request = new UpdateProjectRequest { Name = "Updated Name" };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _projectService.UpdateProjectAsync(Guid.NewGuid(), request, _userId));
    }

    [Fact]
    public async Task UpdateProjectAsync_WithoutWorkspaceAccess_ThrowsForbiddenException()
    {
        // Arrange
        var otherWorkspaceId = Guid.NewGuid();
        _workspaceServiceMock.Setup(x => x.UserHasAccessAsync(otherWorkspaceId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var project = new Project
        {
            Id = Guid.NewGuid(),
            WorkspaceId = otherWorkspaceId,
            Name = "Unauthorized Project"
        };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var request = new UpdateProjectRequest { Name = "Updated Name" };

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(
            () => _projectService.UpdateProjectAsync(project.Id, request, _userId));
    }

    [Fact]
    public async Task UpdateProjectAsync_WithDuplicateName_ThrowsAlreadyExistsException()
    {
        // Arrange
        await CreateTestWorkspaceAsync();
        await CreateTestProjectAsync("Project One");
        var projectTwo = await CreateTestProjectAsync("Project Two");

        var request = new UpdateProjectRequest { Name = "Project One" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AlreadyExistsException>(
            () => _projectService.UpdateProjectAsync(projectTwo.Id, request, _userId));

        exception.Message.Should().Contain("already exists");
    }

    [Fact]
    public async Task UpdateProjectAsync_LogsActivityWithOldAndNewValues()
    {
        // Arrange
        await CreateTestWorkspaceAsync();
        var project = await CreateTestProjectAsync("Original Name");

        var request = new UpdateProjectRequest
        {
            Name = "Updated Name",
            Status = "Active"
        };

        // Act
        await _projectService.UpdateProjectAsync(project.Id, request, _userId);

        // Assert
        _activityServiceMock.Verify(x => x.LogActivityAsync(
            _userId,
            It.IsAny<string?>(),
            "Project",
            project.Id,
            "Updated Name",
            project.Id,
            "ProjectUpdated",
            It.IsAny<string>(),
            It.IsAny<object>(),
            It.IsAny<object>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Archive/Restore Tests

    [Fact]
    public async Task ArchiveProjectAsync_SetsStatusToArchived()
    {
        // Arrange
        await CreateTestWorkspaceAsync();
        var project = await CreateTestProjectAsync("Project To Archive", ProjectStatus.Active);

        // Act
        var result = await _projectService.ArchiveProjectAsync(project.Id, _userId);

        // Assert
        result.Should().BeTrue();

        var dbProject = await _context.Projects.FindAsync(project.Id);
        dbProject!.Status.Should().Be(ProjectStatus.Archived);
    }

    [Fact]
    public async Task ArchiveProjectAsync_LogsProjectArchivedActivity()
    {
        // Arrange
        await CreateTestWorkspaceAsync();
        var project = await CreateTestProjectAsync("Project To Archive");

        // Act
        await _projectService.ArchiveProjectAsync(project.Id, _userId);

        // Assert
        _activityServiceMock.Verify(x => x.LogActivityAsync(
            _userId,
            It.IsAny<string?>(),
            "Project",
            project.Id,
            "Project To Archive",
            project.Id,
            "ProjectArchived",
            It.IsAny<string>(),
            It.IsAny<object?>(),
            It.IsAny<object?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RestoreProjectAsync_SetsStatusToActive()
    {
        // Arrange
        await CreateTestWorkspaceAsync();
        var project = await CreateTestProjectAsync("Archived Project", ProjectStatus.Archived);

        // Act
        var result = await _projectService.RestoreProjectAsync(project.Id, _userId);

        // Assert
        result.Should().BeTrue();

        var dbProject = await _context.Projects.FindAsync(project.Id);
        dbProject!.Status.Should().Be(ProjectStatus.Active);
    }

    [Fact]
    public async Task RestoreProjectAsync_LogsProjectRestoredActivity()
    {
        // Arrange
        await CreateTestWorkspaceAsync();
        var project = await CreateTestProjectAsync("Archived Project", ProjectStatus.Archived);

        // Act
        await _projectService.RestoreProjectAsync(project.Id, _userId);

        // Assert
        _activityServiceMock.Verify(x => x.LogActivityAsync(
            _userId,
            It.IsAny<string?>(),
            "Project",
            project.Id,
            "Archived Project",
            project.Id,
            "ProjectRestored",
            It.IsAny<string>(),
            It.IsAny<object?>(),
            It.IsAny<object?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region DeleteProject Tests

    [Fact]
    public async Task DeleteProjectAsync_PerformsSoftDelete()
    {
        // Arrange
        await CreateTestWorkspaceAsync();
        var project = await CreateTestProjectAsync("Project To Delete");

        // Act
        var result = await _projectService.DeleteProjectAsync(project.Id, _userId);

        // Assert
        result.Should().BeTrue();

        var dbProject = await _context.Projects.FindAsync(project.Id);
        dbProject!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteProjectAsync_WithoutWorkspaceAccess_ThrowsForbiddenException()
    {
        // Arrange
        var otherWorkspaceId = Guid.NewGuid();
        _workspaceServiceMock.Setup(x => x.UserHasAccessAsync(otherWorkspaceId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var project = new Project
        {
            Id = Guid.NewGuid(),
            WorkspaceId = otherWorkspaceId,
            Name = "Unauthorized Delete"
        };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(
            () => _projectService.DeleteProjectAsync(project.Id, _userId));
    }

    [Fact]
    public async Task DeleteProjectAsync_LogsProjectDeletedActivity()
    {
        // Arrange
        await CreateTestWorkspaceAsync();
        var project = await CreateTestProjectAsync("Project To Delete");

        // Act
        await _projectService.DeleteProjectAsync(project.Id, _userId);

        // Assert
        _activityServiceMock.Verify(x => x.LogActivityAsync(
            _userId,
            It.IsAny<string?>(),
            "Project",
            project.Id,
            "Project To Delete",
            project.Id,
            "ProjectDeleted",
            It.IsAny<string>(),
            It.IsAny<object?>(),
            It.IsAny<object?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetWorkspaceProjects Tests

    [Fact]
    public async Task GetWorkspaceProjectsAsync_ReturnsProjectsWithoutArchived()
    {
        // Arrange
        await CreateTestWorkspaceAsync();
        await CreateTestProjectAsync("Active Project", ProjectStatus.Active);
        await CreateTestProjectAsync("Archived Project", ProjectStatus.Archived);

        // Act
        var results = await _projectService.GetWorkspaceProjectsAsync(_workspaceId, _userId);

        // Assert
        results.Should().HaveCount(1);
        results[0].Name.Should().Be("Active Project");
    }

    [Fact]
    public async Task GetWorkspaceProjectsAsync_IncludeArchived_ReturnsAllProjects()
    {
        // Arrange
        await CreateTestWorkspaceAsync();
        await CreateTestProjectAsync("Active Project", ProjectStatus.Active);
        await CreateTestProjectAsync("Archived Project", ProjectStatus.Archived);

        // Act
        var results = await _projectService.GetWorkspaceProjectsAsync(_workspaceId, _userId, includeArchived: true);

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetWorkspaceProjectsAsync_WithoutWorkspaceAccess_ThrowsForbiddenException()
    {
        // Arrange
        var otherWorkspaceId = Guid.NewGuid();
        _workspaceServiceMock.Setup(x => x.UserHasAccessAsync(otherWorkspaceId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(
            () => _projectService.GetWorkspaceProjectsAsync(otherWorkspaceId, _userId));
    }

    #endregion

    #region GetProjectById Tests

    [Fact]
    public async Task GetProjectByIdAsync_ReturnsProjectForAuthorizedUser()
    {
        // Arrange
        await CreateTestWorkspaceAsync();
        var project = await CreateTestProjectAsync("Test Project");

        // Act
        var result = await _projectService.GetProjectByIdAsync(project.Id, _userId);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Project");
    }

    [Fact]
    public async Task GetProjectByIdAsync_ReturnsNullForUnauthorizedUser()
    {
        // Arrange
        await CreateTestWorkspaceAsync();
        var project = await CreateTestProjectAsync("Private Project");
        var unauthorizedUserId = Guid.NewGuid();

        _workspaceServiceMock.Setup(x => x.UserHasAccessAsync(_workspaceId, unauthorizedUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _projectService.GetProjectByIdAsync(project.Id, unauthorizedUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetProjectByIdAsync_ReturnsNullForNonExistentProject()
    {
        // Act
        var result = await _projectService.GetProjectByIdAsync(Guid.NewGuid(), _userId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region UserHasAccessAsync Tests

    [Fact]
    public async Task UserHasAccessAsync_WithWorkspaceAccess_ReturnsTrue()
    {
        // Arrange
        await CreateTestWorkspaceAsync();
        var project = await CreateTestProjectAsync();

        // Act
        var result = await _projectService.UserHasAccessAsync(project.Id, _userId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task UserHasAccessAsync_WithoutWorkspaceAccess_ReturnsFalse()
    {
        // Arrange
        var otherWorkspaceId = Guid.NewGuid();
        var project = new Project
        {
            Id = Guid.NewGuid(),
            WorkspaceId = otherWorkspaceId,
            Name = "Other Workspace Project"
        };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        _workspaceServiceMock.Setup(x => x.UserHasAccessAsync(otherWorkspaceId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _projectService.UserHasAccessAsync(project.Id, _userId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UserHasAccessAsync_NonExistentProject_ReturnsFalse()
    {
        // Act
        var result = await _projectService.UserHasAccessAsync(Guid.NewGuid(), _userId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
