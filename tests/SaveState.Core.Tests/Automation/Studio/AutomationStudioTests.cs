using FluentAssertions;
using Moq;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Automation.Studio;
using SaveState.Infrastructure.Automation.Studio;
using Xunit;

namespace SaveState.Core.Tests.Automation.Studio;

public class AutomationStudioServiceTests
{
    private readonly Mock<IWorkflowEngine> _workflowEngineMock;
    private readonly Mock<ITimeProvider> _timeProviderMock;
    private readonly AutomationStudioService _service;

    public AutomationStudioServiceTests()
    {
        _workflowEngineMock = new Mock<IWorkflowEngine>();
        _timeProviderMock = new Mock<ITimeProvider>();
        _timeProviderMock.Setup(t => t.UtcNow).Returns(DateTime.UtcNow);

        _service = new AutomationStudioService(
            _workflowEngineMock.Object,
            _timeProviderMock.Object,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<AutomationStudioService>>());
    }

    [Fact]
    public async Task CreateWorkflowAsync_WithValidRequest_ReturnsWorkflow()
    {
        // Arrange
        var trigger = new WorkflowTrigger(TriggerType.GameLaunched, "Game Launched", new Dictionary<string, object>());
        var request = new CreateWorkflowRequest(
            "Test Workflow",
            "Test Description",
            trigger,
            new List<WorkflowAction>());

        // Act
        var result = await _service.CreateWorkflowAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Test Workflow");
        result.Value.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task CreateWorkflowAsync_WithNullName_ReturnsFailure()
    {
        // Arrange
        var request = new CreateWorkflowRequest(
            null!,
            "Test Description",
            new WorkflowTrigger(TriggerType.GameLaunched, "Game Launched", new Dictionary<string, object>()),
            new List<WorkflowAction>());

        // Act
        var result = await _service.CreateWorkflowAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task GetWorkflowAsync_WithExistingId_ReturnsWorkflow()
    {
        // Arrange
        var trigger = new WorkflowTrigger(TriggerType.GameLaunched, "Game Launched", new Dictionary<string, object>());
        var createRequest = new CreateWorkflowRequest(
            "Test Workflow",
            null,
            trigger,
            new List<WorkflowAction>());

        var created = await _service.CreateWorkflowAsync(createRequest);
        var workflowId = created.Value!.Id;

        // Act
        var result = await _service.GetWorkflowAsync(workflowId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(workflowId);
    }

    [Fact]
    public async Task GetWorkflowAsync_WithNonExistingId_ReturnsFailure()
    {
        // Act
        var result = await _service.GetWorkflowAsync("non-existing-id");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task ActivateWorkflowAsync_WithExistingId_ReturnsSuccess()
    {
        // Arrange
        var trigger = new WorkflowTrigger(TriggerType.GameLaunched, "Game Launched", new Dictionary<string, object>());
        var createRequest = new CreateWorkflowRequest(
            "Test Workflow",
            null,
            trigger,
            new List<WorkflowAction>());

        var created = await _service.CreateWorkflowAsync(createRequest);
        var workflowId = created.Value!.Id;

        // Act
        var result = await _service.ActivateWorkflowAsync(workflowId);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var workflow = await _service.GetWorkflowAsync(workflowId);
        workflow.Value!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateWorkflowAsync_WithActiveWorkflow_ReturnsSuccess()
    {
        // Arrange
        var trigger = new WorkflowTrigger(TriggerType.GameLaunched, "Game Launched", new Dictionary<string, object>());
        var createRequest = new CreateWorkflowRequest(
            "Test Workflow",
            null,
            trigger,
            new List<WorkflowAction>());

        var created = await _service.CreateWorkflowAsync(createRequest);
        var workflowId = created.Value!.Id;
        await _service.ActivateWorkflowAsync(workflowId);

        // Act
        var result = await _service.DeactivateWorkflowAsync(workflowId);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var workflow = await _service.GetWorkflowAsync(workflowId);
        workflow.Value!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteWorkflowAsync_WithExistingId_ReturnsSuccess()
    {
        // Arrange
        var trigger = new WorkflowTrigger(TriggerType.GameLaunched, "Game Launched", new Dictionary<string, object>());
        var createRequest = new CreateWorkflowRequest(
            "Test Workflow",
            null,
            trigger,
            new List<WorkflowAction>());

        var created = await _service.CreateWorkflowAsync(createRequest);
        var workflowId = created.Value!.Id;

        // Act
        var result = await _service.DeleteWorkflowAsync(workflowId);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var getResult = await _service.GetWorkflowAsync(workflowId);
        getResult.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ListWorkflowsAsync_ReturnsAllWorkflows()
    {
        // Arrange
        var trigger = new WorkflowTrigger(TriggerType.GameLaunched, "Game Launched", new Dictionary<string, object>());
        await _service.CreateWorkflowAsync(new CreateWorkflowRequest("Workflow 1", null, trigger, new List<WorkflowAction>()));
        await _service.CreateWorkflowAsync(new CreateWorkflowRequest("Workflow 2", null, trigger, new List<WorkflowAction>()));

        // Act
        var result = await _service.ListWorkflowsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListWorkflowsAsync_WithFilter_ReturnsFilteredWorkflows()
    {
        // Arrange
        var trigger = new WorkflowTrigger(TriggerType.GameLaunched, "Game Launched", new Dictionary<string, object>());
        await _service.CreateWorkflowAsync(new CreateWorkflowRequest("Active Workflow", null, trigger, new List<WorkflowAction>()));
        await _service.CreateWorkflowAsync(new CreateWorkflowRequest("Inactive Workflow", null, trigger, new List<WorkflowAction>()));

        var workflows = await _service.ListWorkflowsAsync();
        var activeWorkflow = workflows.Value!.First();
        await _service.ActivateWorkflowAsync(activeWorkflow.Id);

        // Act
        var result = await _service.ListWorkflowsAsync(new WorkflowFilter(IsActive: true));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value!.First().IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateWorkflowAsync_WithValidWorkflow_ReturnsValid()
    {
        // Arrange
        var trigger = new WorkflowTrigger(TriggerType.GameLaunched, "Game Launched", new Dictionary<string, object>());
        var workflow = new Workflow(
            "test-id",
            "Test Workflow",
            null,
            trigger,
            new List<WorkflowAction>(),
            null,
            false,
            DateTime.UtcNow);

        // Act
        var result = await _service.ValidateWorkflowAsync(workflow);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateWorkflowAsync_WithEmptyName_ReturnsInvalid()
    {
        // Arrange
        var trigger = new WorkflowTrigger(TriggerType.GameLaunched, "Game Launched", new Dictionary<string, object>());
        var workflow = new Workflow(
            "test-id",
            "",
            null,
            trigger,
            new List<WorkflowAction>(),
            null,
            false,
            DateTime.UtcNow);

        // Act
        var result = await _service.ValidateWorkflowAsync(workflow);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsValid.Should().BeFalse();
        result.Value.Issues.Should().Contain(i => i.Message.Contains("name"));
    }

    [Fact]
    public async Task DuplicateWorkflowAsync_ReturnsNewWorkflowWithCopy()
    {
        // Arrange
        var trigger = new WorkflowTrigger(TriggerType.GameLaunched, "Game Launched", new Dictionary<string, object>());
        var created = await _service.CreateWorkflowAsync(new CreateWorkflowRequest(
            "Original Workflow",
            null,
            trigger,
            new List<WorkflowAction>()));

        // Act
        var result = await _service.DuplicateWorkflowAsync(created.Value!.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Original Workflow (Copy)");
        result.Value.Id.Should().NotBe(created.Value.Id);
        result.Value.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetAvailableTriggersAsync_ReturnsTriggerDefinitions()
    {
        // Act
        var result = await _service.GetAvailableTriggersAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        result.Value.Should().Contain(t => t.Type == TriggerType.GameLaunched);
        result.Value.Should().Contain(t => t.Type == TriggerType.TimeOfDay);
    }

    [Fact]
    public async Task GetAvailableActionsAsync_ReturnsActionDefinitions()
    {
        // Act
        var result = await _service.GetAvailableActionsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        result.Value.Should().Contain(a => a.Type == ActionType.LaunchGame);
        result.Value.Should().Contain(a => a.Type == ActionType.SendNotification);
    }
}

public class WorkflowEngineTests
{
    private readonly Mock<ITimeProvider> _timeProviderMock;
    private readonly WorkflowEngine _engine;

    public WorkflowEngineTests()
    {
        _timeProviderMock = new Mock<ITimeProvider>();
        _timeProviderMock.Setup(t => t.UtcNow).Returns(DateTime.UtcNow);
        _timeProviderMock.Setup(t => t.GetTimestamp()).Returns(() => System.Diagnostics.Stopwatch.GetTimestamp());

        _engine = new WorkflowEngine(
            _timeProviderMock.Object,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<WorkflowEngine>>());
    }

    [Fact]
    public async Task ExecuteAsync_WithValidWorkflow_ReturnsSuccess()
    {
        // Arrange
        var workflow = CreateTestWorkflow();
        var context = new WorkflowContext(TriggerType.GameLaunched, new Dictionary<string, object>(), DateTime.UtcNow);

        // Act
        var result = await _engine.ExecuteAsync(workflow, context);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(WorkflowStatus.Completed);
    }

    [Fact]
    public async Task ExecuteAsync_WithFailingCondition_SkipsActions()
    {
        // Arrange
        var workflow = CreateTestWorkflowWithCondition("false");
        var context = new WorkflowContext(TriggerType.GameLaunched, new Dictionary<string, object>(), DateTime.UtcNow);

        // Act
        var result = await _engine.ExecuteAsync(workflow, context);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(WorkflowStatus.Completed);
    }

    [Fact]
    public async Task EvaluateConditionAsync_WithTrueExpression_ReturnsTrue()
    {
        // Arrange
        var condition = new WorkflowCondition(ConditionType.Expression, "true");
        var context = new WorkflowContext(TriggerType.GameLaunched, new Dictionary<string, object>(), DateTime.UtcNow);

        // Act
        var result = await _engine.EvaluateConditionAsync(condition, context);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterTriggerAsync_AddsListener()
    {
        // Arrange
        var listener = new TriggerListener("listener1", "workflow1");

        // Act
        var result = await _engine.RegisterTriggerAsync(TriggerType.GameLaunched, listener);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetStatisticsAsync_ReturnsStatistics()
    {
        // Act
        var result = await _engine.GetStatisticsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    private static Workflow CreateTestWorkflow()
    {
        return new Workflow(
            "test-id",
            "Test Workflow",
            null,
            new WorkflowTrigger(TriggerType.GameLaunched, "Game Launched", new Dictionary<string, object>()),
            new List<WorkflowAction>
            {
                new("action1", ActionType.SendNotification, "Send Notification", new Dictionary<string, object>()),
                new("action2", ActionType.Delay, "Wait", new Dictionary<string, object> { ["milliseconds"] = 10 })
            },
            null,
            true,
            DateTime.UtcNow);
    }

    private static Workflow CreateTestWorkflowWithCondition(string expression)
    {
        return new Workflow(
            "test-id",
            "Test Workflow",
            null,
            new WorkflowTrigger(TriggerType.GameLaunched, "Game Launched", new Dictionary<string, object>()),
            new List<WorkflowAction>
            {
                new("action1", ActionType.SendNotification, "Send Notification", new Dictionary<string, object>())
            },
            new WorkflowCondition(ConditionType.Expression, expression),
            true,
            DateTime.UtcNow);
    }
}
