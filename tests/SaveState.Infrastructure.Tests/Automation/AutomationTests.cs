using FluentAssertions;
using Moq;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Automation.Studio;
using SaveState.Infrastructure.Automation.Studio;
using Xunit;

namespace SaveState.Infrastructure.Tests.Automation;

public class AutomationStudioIntegrationTests
{
    private readonly Mock<IWorkflowEngine> _workflowEngineMock;
    private readonly Mock<ITimeProvider> _timeProviderMock;
    private readonly AutomationStudioService _service;

    public AutomationStudioIntegrationTests()
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
    public async Task CompleteWorkflowLifecycle_CreateActivateTriggerDeactivate()
    {
        // Arrange
        var trigger = new WorkflowTrigger(TriggerType.GameLaunched, "Game Launched", new Dictionary<string, object>());
        var actions = new List<WorkflowAction>
        {
            new("action1", ActionType.SendNotification, "Notify", new Dictionary<string, object>())
        };
        var createRequest = new CreateWorkflowRequest("Integration Test", null, trigger, actions);

        // Act - Create
        var created = await _service.CreateWorkflowAsync(createRequest);
        created.IsSuccess.Should().BeTrue();
        var workflowId = created.Value!.Id;

        // Act - Verify inactive by default
        var workflow = await _service.GetWorkflowAsync(workflowId);
        workflow.Value!.IsActive.Should().BeFalse();

        // Act - Activate
        var activateResult = await _service.ActivateWorkflowAsync(workflowId);
        activateResult.IsSuccess.Should().BeTrue();

        workflow = await _service.GetWorkflowAsync(workflowId);
        workflow.Value!.IsActive.Should().BeTrue();

        // Act - Trigger (simulated)
        var triggerResult = await _service.TriggerWorkflowAsync(workflowId);
        triggerResult.IsSuccess.Should().BeTrue();
        triggerResult.Value!.Success.Should().BeTrue();

        // Act - Deactivate
        var deactivateResult = await _service.DeactivateWorkflowAsync(workflowId);
        deactivateResult.IsSuccess.Should().BeTrue();

        workflow = await _service.GetWorkflowAsync(workflowId);
        workflow.Value!.IsActive.Should().BeFalse();

        // Act - Verify execution history
        var history = await _service.GetExecutionHistoryAsync(workflowId);
        history.IsSuccess.Should().BeTrue();
        history.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task ExportImportWorkflow_MaintainsConfiguration()
    {
        // Arrange
        var trigger = new WorkflowTrigger(TriggerType.TimeOfDay, "Time of Day", new Dictionary<string, object> { ["hour"] = 20 });
        var actions = new List<WorkflowAction>
        {
            new("action1", ActionType.EnableBlueLightFilter, "Blue Light", new Dictionary<string, object> { ["intensity"] = 70 })
        };
        var createRequest = new CreateWorkflowRequest("Export Test", "Test Description", trigger, actions);

        var created = await _service.CreateWorkflowAsync(createRequest);
        var workflowId = created.Value!.Id;

        // Act - Export
        var exportResult = await _service.ExportWorkflowAsync(workflowId);
        exportResult.IsSuccess.Should().BeTrue();
        var json = exportResult.Value!;

        // Act - Import
        var importResult = await _service.ImportWorkflowAsync(json);
        importResult.IsSuccess.Should().BeTrue();

        // Assert - Verify imported workflow
        var imported = importResult.Value!;
        imported.Id.Should().NotBe(workflowId);
        imported.Name.Should().Be("Export Test");
        imported.Description.Should().Be("Test Description");
        imported.Trigger.Type.Should().Be(TriggerType.TimeOfDay);
        imported.Actions.Should().HaveCount(1);
        imported.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetAvailableTriggers_ContainsExpectedTypes()
    {
        // Act
        var result = await _service.GetAvailableTriggersAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        var triggers = result.Value!;

        triggers.Should().Contain(t => t.Type == TriggerType.GameLaunched && t.Category == "Game Events");
        triggers.Should().Contain(t => t.Type == TriggerType.GameClosed && t.Category == "Game Events");
        triggers.Should().Contain(t => t.Type == TriggerType.AchievementUnlocked && t.Category == "Game Events");
        triggers.Should().Contain(t => t.Type == TriggerType.SessionStarted && t.Category == "Game Events");
        triggers.Should().Contain(t => t.Type == TriggerType.SessionEnded && t.Category == "Game Events");
        triggers.Should().Contain(t => t.Type == TriggerType.TimeOfDay && t.Category == "Time-based");
        triggers.Should().Contain(t => t.Type == TriggerType.DayOfWeek && t.Category == "Time-based");
    }

    [Fact]
    public async Task GetAvailableActions_ContainsExpectedTypes()
    {
        // Act
        var result = await _service.GetAvailableActionsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        var actions = result.Value!;

        // Game Actions
        actions.Should().Contain(a => a.Type == ActionType.LaunchGame && a.Category == "Game Actions");

        // System Actions
        actions.Should().Contain(a => a.Type == ActionType.EnableBlueLightFilter && a.Category == "System Actions");
        actions.Should().Contain(a => a.Type == ActionType.SetDoNotDisturb && a.Category == "System Actions");
        actions.Should().Contain(a => a.Type == ActionType.AdjustVolume && a.Category == "System Actions");
        actions.Should().Contain(a => a.Type == ActionType.EnablePerformanceMode && a.Category == "System Actions");

        // Notification Actions
        actions.Should().Contain(a => a.Type == ActionType.SendNotification && a.Category == "Notification Actions");
        actions.Should().Contain(a => a.Type == ActionType.PostToDiscord && a.Category == "Notification Actions");

        // Recording Actions
        actions.Should().Contain(a => a.Type == ActionType.StartRecording && a.Category == "Recording Actions");
    }

    [Fact]
    public async Task WorkflowFilter_ByTriggerType_ReturnsMatchingWorkflows()
    {
        // Arrange
        var gameTrigger = new WorkflowTrigger(TriggerType.GameLaunched, "Game", new Dictionary<string, object>());
        var timeTrigger = new WorkflowTrigger(TriggerType.TimeOfDay, "Time", new Dictionary<string, object>());

        await _service.CreateWorkflowAsync(new CreateWorkflowRequest("Game Workflow", null, gameTrigger, new List<WorkflowAction>()));
        await _service.CreateWorkflowAsync(new CreateWorkflowRequest("Time Workflow", null, timeTrigger, new List<WorkflowAction>()));

        // Act
        var result = await _service.ListWorkflowsAsync(new WorkflowFilter(TriggerType: TriggerType.GameLaunched));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value!.First().TriggerType.Should().Be(TriggerType.GameLaunched.ToString());
    }

    [Fact]
    public async Task WorkflowFilter_BySearchTerm_ReturnsMatchingWorkflows()
    {
        // Arrange
        var trigger = new WorkflowTrigger(TriggerType.GameLaunched, "Game", new Dictionary<string, object>());
        await _service.CreateWorkflowAsync(new CreateWorkflowRequest("Alpha Workflow", null, trigger, new List<WorkflowAction>()));
        await _service.CreateWorkflowAsync(new CreateWorkflowRequest("Beta Workflow", null, trigger, new List<WorkflowAction>()));

        // Act
        var result = await _service.ListWorkflowsAsync(new WorkflowFilter(SearchTerm: "Alpha"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value!.First().Name.Should().Contain("Alpha");
    }
}
