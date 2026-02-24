using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SaveState.Presentation.Services;
using SaveState.Presentation.ViewModels.Shell;

namespace SaveState.EndToEndTests;

public class NavigationButtonSmokeTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IServiceProvider _services;

    public NavigationButtonSmokeTests(IntegrationTestFixture fixture)
    {
        _services = fixture.Services;
    }

    [Fact]
    public async Task NavigationService_CanNavigateToLibraryMugenAndGamerDna()
    {
        var navigationService = _services.GetRequiredService<INavigationService>();

        await navigationService.NavigateToAsync("Library");
        navigationService.CurrentTab.Should().Be("Library");

        await navigationService.NavigateToAsync("MUGEN");
        navigationService.CurrentTab.Should().Be("MUGEN");

        await navigationService.NavigateToAsync("Gamer DNA");
        navigationService.CurrentTab.Should().Be("Gamer DNA");
    }

    [Fact]
    public void DashboardCustomizeCommand_ShowsDashboardCustomizationOverlay()
    {
        var overlayService = _services.GetRequiredService<IOverlayService>();
        var dashboardViewModel = _services.GetRequiredService<DashboardViewModel>();

        overlayService.ShowDashboardCustomization.Should().BeFalse();

        dashboardViewModel.CustomizeCommand.Execute(null);

        overlayService.ShowDashboardCustomization.Should().BeTrue();
    }
}
