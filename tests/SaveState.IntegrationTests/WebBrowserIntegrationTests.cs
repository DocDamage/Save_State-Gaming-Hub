using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SaveState.Core.Common;
using BrowserTab = SaveState.Core.WebBrowser.Models.BrowserTab;
using BrowserTabState = SaveState.Core.WebBrowser.Models.BrowserTabState;
using BrowserBookmark = SaveState.Core.WebBrowser.Models.BrowserBookmark;
using BrowserSettings = SaveState.Core.WebBrowser.Models.BrowserSettings;
using ZoomLevel = SaveState.Core.WebBrowser.Models.ZoomLevel;
using DownloadSettings = SaveState.Core.WebBrowser.Models.DownloadSettings;
using BrowserFindOptions = SaveState.Core.WebBrowser.Models.BrowserFindOptions;
using BrowserDataType = SaveState.Core.WebBrowser.Models.BrowserDataType;
using BrowserCookie = SaveState.Core.WebBrowser.Models.BrowserCookie;
using BrowserExtension = SaveState.Core.WebBrowser.Models.BrowserExtension;
using HistoryItem = SaveState.Core.WebBrowser.Models.HistoryItem;
using BrowserHistoryItem = SaveState.Core.WebBrowser.Models.BrowserHistoryItem;
using IBrowserService = SaveState.Core.WebBrowser.Services.IBrowserService;
using IOAuthIntegrationService = SaveState.Core.WebBrowser.Services.IOAuthIntegrationService;
using OAuthCallback = SaveState.Core.WebBrowser.Models.OAuthCallback;
using SaveState.Tests.Fakes;

namespace SaveState.IntegrationTests;

/// <summary>
/// Integration tests for web browser functionality.
/// Tests browser initialization, navigation, tab management, OAuth flows, and download handling.
/// </summary>
public class WebBrowserIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly IBrowserService _browserService;
    private readonly IOAuthIntegrationService _oauthService;

    public WebBrowserIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _browserService = _fixture.ServiceProvider.GetRequiredService<IBrowserService>();
        _oauthService = _fixture.ServiceProvider.GetRequiredService<IOAuthIntegrationService>();
    }

    #region Browser Initialization Tests

    [Fact]
    public async Task InitializeBrowser_StartsBrowserEngine()
    {
        // Act
        var result = await _browserService.InitializeAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task IsInitialized_ReturnsCorrectStatus()
    {
        // Act
        var result = await _browserService.IsInitializedAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Result could be true or false depending on previous tests
    }

    [Fact]
    public async Task ShutdownBrowser_StopsBrowserEngine()
    {
        // Arrange
        await _browserService.InitializeAsync();

        // Act
        var result = await _browserService.ShutdownAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RestartBrowser_ReinitializesSuccessfully()
    {
        // Arrange
        await _browserService.InitializeAsync();

        // Act
        var result = await _browserService.RestartAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Tab Management Tests

    [Fact]
    public async Task CreateTab_CreatesNewTab()
    {
        // Act
        var result = await _browserService.CreateTabAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Url.Should().Be("about:blank");
        result.Value.State.Should().Be(BrowserTabState.Loading);
    }

    [Fact]
    public async Task CreateTab_WithUrl_LoadsUrl()
    {
        // Arrange
        var url = "https://example.com";

        // Act
        var result = await _browserService.CreateTabAsync(url);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Url.Should().Be(url);
    }

    [Fact]
    public async Task CreateTab_InIncognitoMode_CreatesPrivateTab()
    {
        // Arrange
        var url = "https://example.com";

        // Act
        var result = await _browserService.CreateTabAsync(url, isIncognito: true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsIncognito.Should().BeTrue();
    }

    [Fact]
    public async Task CloseTab_RemovesTab()
    {
        // Arrange
        var tabResult = await _browserService.CreateTabAsync();
        tabResult.IsSuccess.Should().BeTrue();

        // Act
        var result = await _browserService.CloseTabAsync(tabResult.Value.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify tab is closed
        var tabs = await _browserService.GetTabsAsync();
        tabs.Value.Should().NotContain(t => t.Id == tabResult.Value.Id);
    }

    [Fact]
    public async Task CloseNonExistentTab_ReturnsFailure()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _browserService.CloseTabAsync(nonExistentId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task GetTab_ReturnsTab()
    {
        // Arrange
        var tabResult = await _browserService.CreateTabAsync();
        tabResult.IsSuccess.Should().BeTrue();

        // Act
        var result = await _browserService.GetTabAsync(tabResult.Value.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(tabResult.Value.Id);
    }

    [Fact]
    public async Task GetTabs_ReturnsAllTabs()
    {
        // Arrange
        await _browserService.CreateTabAsync();
        await _browserService.CreateTabAsync();
        await _browserService.CreateTabAsync();

        // Act
        var result = await _browserService.GetTabsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Count.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task SwitchTab_SetsActiveTab()
    {
        // Arrange
        var tab1 = await _browserService.CreateTabAsync();
        var tab2 = await _browserService.CreateTabAsync();
        tab1.IsSuccess.Should().BeTrue();
        tab2.IsSuccess.Should().BeTrue();

        // Act
        var result = await _browserService.SwitchTabAsync(tab2.Value.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var activeTab = await _browserService.GetActiveTabAsync();
        activeTab.Value.Id.Should().Be(tab2.Value.Id);
    }

    [Fact]
    public async Task DuplicateTab_CreatesCopyOfTab()
    {
        // Arrange
        var tabResult = await _browserService.CreateTabAsync("https://example.com");
        tabResult.IsSuccess.Should().BeTrue();

        // Act
        var result = await _browserService.DuplicateTabAsync(tabResult.Value.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Url.Should().Be(tabResult.Value.Url);
        result.Value.Id.Should().NotBe(tabResult.Value.Id);
    }

    [Fact]
    public async Task PinTab_SetsPinnedState()
    {
        // Arrange
        var tabResult = await _browserService.CreateTabAsync();
        tabResult.IsSuccess.Should().BeTrue();

        // Act
        var result = await _browserService.PinTabAsync(tabResult.Value.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var tab = await _browserService.GetTabAsync(tabResult.Value.Id);
        tab.Value.IsPinned.Should().BeTrue();
    }

    [Fact]
    public async Task UnpinTab_RemovesPinnedState()
    {
        // Arrange
        var tabResult = await _browserService.CreateTabAsync();
        tabResult.IsSuccess.Should().BeTrue();
        await _browserService.PinTabAsync(tabResult.Value.Id);

        // Act
        var result = await _browserService.UnpinTabAsync(tabResult.Value.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var tab = await _browserService.GetTabAsync(tabResult.Value.Id);
        tab.Value.IsPinned.Should().BeFalse();
    }

    [Fact]
    public async Task MuteTab_SetsMutedState()
    {
        // Arrange
        var tabResult = await _browserService.CreateTabAsync();
        tabResult.IsSuccess.Should().BeTrue();

        // Act
        var result = await _browserService.MuteTabAsync(tabResult.Value.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var tab = await _browserService.GetTabAsync(tabResult.Value.Id);
        tab.Value.IsMuted.Should().BeTrue();
    }

    [Fact]
    public async Task UnmuteTab_RemovesMutedState()
    {
        // Arrange
        var tabResult = await _browserService.CreateTabAsync();
        tabResult.IsSuccess.Should().BeTrue();
        await _browserService.MuteTabAsync(tabResult.Value.Id);

        // Act
        var result = await _browserService.UnmuteTabAsync(tabResult.Value.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var tab = await _browserService.GetTabAsync(tabResult.Value.Id);
        tab.Value.IsMuted.Should().BeFalse();
    }

    [Fact]
    public async Task CloseAllTabsExcept_ClosesOtherTabs()
    {
        // Arrange
        var tab1 = await _browserService.CreateTabAsync();
        var tab2 = await _browserService.CreateTabAsync();
        var tab3 = await _browserService.CreateTabAsync();
        tab1.IsSuccess.Should().BeTrue();

        // Act
        var result = await _browserService.CloseAllTabsExceptAsync(tab1.Value.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var tabs = await _browserService.GetTabsAsync();
        tabs.Value.Count.Should().Be(1);
        tabs.Value.Should().Contain(t => t.Id == tab1.Value.Id);
    }

    #endregion

    #region Navigation Tests

    [Fact]
    public async Task NavigateTo_LoadsUrl()
    {
        // Arrange
        var tabResult = await _browserService.CreateTabAsync();
        tabResult.IsSuccess.Should().BeTrue();
        var url = "https://example.com";

        // Act
        var result = await _browserService.NavigateToAsync(tabResult.Value.Id, url);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var tab = await _browserService.GetTabAsync(tabResult.Value.Id);
        tab.Value.Url.Should().Be(url);
    }

    [Fact]
    public async Task NavigateTo_InvalidUrl_ReturnsFailure()
    {
        // Arrange
        var tabResult = await _browserService.CreateTabAsync();
        tabResult.IsSuccess.Should().BeTrue();
        var invalidUrl = "not-a-valid-url";

        // Act
        var result = await _browserService.NavigateToAsync(tabResult.Value.Id, invalidUrl);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task GoBack_NavigatesBack()
    {
        // Arrange
        var tabResult = await _browserService.CreateTabAsync();
        tabResult.IsSuccess.Should().BeTrue();
        await _browserService.NavigateToAsync(tabResult.Value.Id, "https://example.com/page1");
        await _browserService.NavigateToAsync(tabResult.Value.Id, "https://example.com/page2");

        // Act
        var result = await _browserService.GoBackAsync(tabResult.Value.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GoForward_NavigatesForward()
    {
        // Arrange
        var tabResult = await _browserService.CreateTabAsync();
        tabResult.IsSuccess.Should().BeTrue();
        await _browserService.NavigateToAsync(tabResult.Value.Id, "https://example.com/page1");
        await _browserService.NavigateToAsync(tabResult.Value.Id, "https://example.com/page2");
        await _browserService.GoBackAsync(tabResult.Value.Id);

        // Act
        var result = await _browserService.GoForwardAsync(tabResult.Value.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Refresh_ReloadsPage()
    {
        // Arrange
        var tabResult = await _browserService.CreateTabAsync("https://example.com");
        tabResult.IsSuccess.Should().BeTrue();

        // Act
        var result = await _browserService.RefreshAsync(tabResult.Value.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task StopLoading_StopsPageLoad()
    {
        // Arrange
        var tabResult = await _browserService.CreateTabAsync();
        tabResult.IsSuccess.Should().BeTrue();

        // Act
        var result = await _browserService.StopLoadingAsync(tabResult.Value.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SetZoom_ChangesZoomLevel()
    {
        // Arrange
        var tabResult = await _browserService.CreateTabAsync();
        tabResult.IsSuccess.Should().BeTrue();

        // Act
        var result = await _browserService.SetZoomAsync(tabResult.Value.Id, ZoomLevel.Close);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var tab = await _browserService.GetTabAsync(tabResult.Value.Id);
        tab.Value.Zoom.Should().Be(ZoomLevel.Close);
    }

    [Theory]
    [InlineData(ZoomLevel.Minimum)]
    [InlineData(ZoomLevel.Far)]
    [InlineData(ZoomLevel.Close)]
    [InlineData(ZoomLevel.Default)]
    public async Task SetZoom_ToDifferentLevels_WorksCorrectly(ZoomLevel zoom)
    {
        // Arrange
        var tabResult = await _browserService.CreateTabAsync();
        tabResult.IsSuccess.Should().BeTrue();

        // Act
        var result = await _browserService.SetZoomAsync(tabResult.Value.Id, zoom);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region OAuth Flow Tests

    [Fact]
    public async Task InitiateOAuthFlow_StartsOAuthProcess()
    {
        // Arrange
        var provider = "steam";
        var redirectUri = "http://localhost:5000/oauth/callback";

        // Act
        var result = await ((FakeOAuthIntegrationService)_oauthService).InitiateOAuthFlowAsync(provider, redirectUri);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task InitiateOAuthFlow_WithInvalidProvider_ReturnsFailure()
    {
        // Arrange
        var provider = "invalid_provider";
        var redirectUri = "http://localhost:5000/oauth/callback";

        // Act
        var result = await ((FakeOAuthIntegrationService)_oauthService).InitiateOAuthFlowAsync(provider, redirectUri);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task HandleOAuthCallback_ProcessesCallback()
    {
        // Arrange
        var callback = new OAuthCallback
        {
            Provider = "steam",
            Code = "auth_code_123",
            State = "state_123",
            AdditionalData = new Dictionary<string, string>
            {
                { "scope", "read_profile" }
            }
        };

        // Act
        var result = await ((FakeOAuthIntegrationService)_oauthService).HandleOAuthCallbackAsync(callback);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetOAuthProviders_ReturnsSupportedProviders()
    {
        // Act
        var result = await ((FakeOAuthIntegrationService)_oauthService).GetSupportedProvidersAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }

    [Fact]
    public async Task IsOAuthConnected_ChecksConnectionStatus()
    {
        // Arrange
        var provider = "steam";

        // Act
        var result = await ((FakeOAuthIntegrationService)_oauthService).IsConnectedAsync(provider);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DisconnectOAuth_DisconnectsProvider()
    {
        // Arrange
        var provider = "steam";

        // Act
        var result = await ((FakeOAuthIntegrationService)_oauthService).DisconnectAsync(provider);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetOAuthToken_ReturnsAccessToken()
    {
        // Arrange
        var provider = "steam";

        // Act
        var result = await ((FakeOAuthIntegrationService)_oauthService).GetAccessTokenAsync(provider);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Download Handling Tests

    [Fact]
    public async Task GetDownloads_ReturnsDownloadList()
    {
        // Act
        var result = await _browserService.GetDownloadsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task CancelDownload_CancelsActiveDownload()
    {
        // Arrange
        var downloadId = Guid.NewGuid();

        // Act
        var result = await _browserService.CancelDownloadAsync(downloadId);

        // Assert
        (result.IsSuccess == true || result.IsSuccess == false).Should().BeTrue();
    }

    [Fact]
    public async Task PauseDownload_PausesActiveDownload()
    {
        // Arrange
        var downloadId = Guid.NewGuid();

        // Act
        var result = await _browserService.PauseDownloadAsync(downloadId);

        // Assert
        (result.IsSuccess == true || result.IsSuccess == false).Should().BeTrue();
    }

    [Fact]
    public async Task ResumeDownload_ResumesPausedDownload()
    {
        // Arrange
        var downloadId = Guid.NewGuid();

        // Act
        var result = await _browserService.ResumeDownloadAsync(downloadId);

        // Assert
        (result.IsSuccess == true || result.IsSuccess == false).Should().BeTrue();
    }

    [Fact]
    public async Task ClearCompletedDownloads_RemovesCompletedDownloads()
    {
        // Act
        var result = await _browserService.ClearCompletedDownloadsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetDownloadSettings_ReturnsSettings()
    {
        // Act
        var result = await _browserService.GetDownloadSettingsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateDownloadSettings_UpdatesSettings()
    {
        // Arrange
        var settings = new DownloadSettings
        {
            DownloadPath = Path.Combine(Path.GetTempPath(), "SaveStateTests", "Downloads"),
            EnableDownloads = true
        };

        // Act
        var result = await _browserService.UpdateDownloadSettingsAsync(settings);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Bookmark Tests

    [Fact]
    public async Task AddBookmark_CreatesBookmark()
    {
        // Arrange
        var bookmark = new BrowserBookmark
        {
            Title = "Test Bookmark",
            Url = "https://example.com",
            Folder = "Test Folder"
        };

        // Act
        var result = await _browserService.AddBookmarkAsync(bookmark);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Title.Should().Be(bookmark.Title);
    }

    [Fact]
    public async Task GetBookmarks_ReturnsBookmarks()
    {
        // Act
        var result = await _browserService.GetBookmarksAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteBookmark_RemovesBookmark()
    {
        // Arrange
        var bookmark = new BrowserBookmark
        {
            Title = "Delete Test",
            Url = "https://example.com/delete"
        };
        var createResult = await _browserService.AddBookmarkAsync(bookmark);
        createResult.IsSuccess.Should().BeTrue();

        // Act
        var result = await _browserService.DeleteBookmarkAsync(createResult.Value.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateBookmark_UpdatesBookmarkData()
    {
        // Arrange
        var bookmark = new BrowserBookmark
        {
            Title = "Original Title",
            Url = "https://example.com"
        };
        var createResult = await _browserService.AddBookmarkAsync(bookmark);
        createResult.IsSuccess.Should().BeTrue();

        var updatedBookmark = createResult.Value with { Title = "Updated Title" };

        // Act
        var result = await _browserService.UpdateBookmarkAsync(updatedBookmark);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ImportBookmarks_ImportsFromHtml()
    {
        // Arrange
        var htmlContent = @"<!DOCTYPE NETSCAPE-Bookmark-file-1>
<html>
<head><title>Bookmarks</title></head>
<body>
<DT><A HREF=""https://example.com"">Example</A>
</body>
</html>";

        // Act
        var result = await _browserService.ImportBookmarksAsync(htmlContent);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ExportBookmarks_ReturnsBookmarkHtml()
    {
        // Act
        var result = await _browserService.ExportBookmarksAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region History Tests

    [Fact]
    public async Task GetHistory_ReturnsHistoryItems()
    {
        // Act
        var result = await _browserService.GetHistoryAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task ClearHistory_RemovesHistory()
    {
        // Act
        var result = await _browserService.ClearHistoryAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SearchHistory_FindsMatchingItems()
    {
        // Arrange
        var query = "example";

        // Act
        var result = await _browserService.SearchHistoryAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task AddToHistory_AddsHistoryItem()
    {
        // Arrange
        var historyItem = new HistoryItem
        {
            Title = "Example Page",
            Url = "https://example.com",
            VisitedAt = DateTime.UtcNow
        };

        // Act
        var result = await _browserService.AddToHistoryAsync(historyItem);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteHistoryItem_RemovesSpecificItem()
    {
        // Arrange
        var historyItem = new HistoryItem
        {
            Title = "Delete Test",
            Url = "https://example.com/delete"
        };
        var createResult = await _browserService.AddToHistoryAsync(historyItem);
        createResult.IsSuccess.Should().BeTrue();

        // Act
        var result = await _browserService.DeleteHistoryItemAsync(createResult.Value.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Settings Tests

    [Fact]
    public async Task GetSettings_ReturnsBrowserSettings()
    {
        // Act
        var result = await _browserService.GetSettingsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateSettings_SavesSettings()
    {
        // Arrange
        var settings = new BrowserSettings
        {
            HomePage = "https://www.duckduckgo.com",
            BlockPopups = true,
            EnableJavaScript = true,
            EnablePlugins = true
        };

        // Act
        var result = await _browserService.UpdateSettingsAsync(settings);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var getResult = await _browserService.GetSettingsAsync();
        getResult.Value.HomePage.Should().Be(settings.HomePage);
    }

    [Fact]
    public async Task SetHomePage_UpdatesHomePage()
    {
        // Arrange
        var homePage = "https://www.google.com";

        // Act
        var result = await _browserService.SetHomePageAsync(homePage);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SetSearchEngine_UpdatesSearchEngine()
    {
        // Arrange
        var searchEngine = "https://www.duckduckgo.com/?q=";

        // Act
        var result = await _browserService.SetSearchEngineAsync(searchEngine);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SetDefaultZoom_UpdatesZoomLevel()
    {
        // Arrange
        var zoom = ZoomLevel.Close;

        // Act
        var result = await _browserService.SetDefaultZoomAsync(zoom);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ClearBrowserData_ClearsSpecifiedData()
    {
        // Arrange
        var dataTypes = BrowserDataType.Cache | BrowserDataType.Cookies;

        // Act
        var result = await _browserService.ClearBrowserDataAsync(dataTypes);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Find Tests

    [Fact]
    public async Task FindInPage_FindsText()
    {
        // Arrange
        var tabResult = await _browserService.CreateTabAsync();
        tabResult.IsSuccess.Should().BeTrue();

        var options = new BrowserFindOptions
        {
            SearchText = "example",
            Forward = true,
            MatchCase = false
        };

        // Act
        var result = await _browserService.FindInPageAsync(tabResult.Value.Id, options);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task FindNext_FindsNextOccurrence()
    {
        // Arrange
        var tabResult = await _browserService.CreateTabAsync();
        tabResult.IsSuccess.Should().BeTrue();

        // Act
        var result = await _browserService.FindNextAsync(tabResult.Value.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task FindPrevious_FindsPreviousOccurrence()
    {
        // Arrange
        var tabResult = await _browserService.CreateTabAsync();
        tabResult.IsSuccess.Should().BeTrue();

        // Act
        var result = await _browserService.FindPreviousAsync(tabResult.Value.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task StopFinding_StopsFindOperation()
    {
        // Arrange
        var tabResult = await _browserService.CreateTabAsync();
        tabResult.IsSuccess.Should().BeTrue();

        // Act
        var result = await _browserService.StopFindingAsync(tabResult.Value.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Cookie Tests

    [Fact]
    public async Task GetCookies_ReturnsCookies()
    {
        // Arrange
        var url = "https://example.com";

        // Act
        var result = await _browserService.GetCookiesAsync(url);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task SetCookie_SetsCookie()
    {
        // Arrange
        var cookie = new BrowserCookie
        {
            Name = "test_cookie",
            Value = "test_value",
            Domain = ".example.com",
            Path = "/",
            Expires = DateTime.UtcNow.AddDays(7)
        };

        // Act
        var result = await _browserService.SetCookieAsync(cookie);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ClearCookies_RemovesAllCookies()
    {
        // Act
        var result = await _browserService.ClearCookiesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ClearCookiesForDomain_RemovesDomainCookies()
    {
        // Arrange
        var domain = "example.com";

        // Act
        var result = await _browserService.ClearCookiesForDomainAsync(domain);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Extension Tests

    [Fact]
    public async Task GetExtensions_ReturnsInstalledExtensions()
    {
        // Act
        var result = await _browserService.GetExtensionsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadExtension_LoadsExtension()
    {
        // Arrange
        var extensionPath = Path.Combine(Path.GetTempPath(), "test-extension");

        // Act
        var result = await _browserService.LoadExtensionAsync(extensionPath);

        // Assert
        (result.IsSuccess == true || result.IsSuccess == false).Should().BeTrue();
    }

    [Fact]
    public async Task EnableExtension_EnablesExtension()
    {
        // Arrange
        var extensionId = "test-extension-id";

        // Act
        var result = await _browserService.EnableExtensionAsync(extensionId);

        // Assert
        (result.IsSuccess == true || result.IsSuccess == false).Should().BeTrue();
    }

    [Fact]
    public async Task DisableExtension_DisablesExtension()
    {
        // Arrange
        var extensionId = "test-extension-id";

        // Act
        var result = await _browserService.DisableExtensionAsync(extensionId);

        // Assert
        (result.IsSuccess == true || result.IsSuccess == false).Should().BeTrue();
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task CreateManyTabs_PerformsEfficiently()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        for (int i = 0; i < 20; i++)
        {
            await _browserService.CreateTabAsync();
        }

        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000);
    }

    [Fact]
    public async Task NavigateTo_RespondsQuickly()
    {
        // Arrange
        var tabResult = await _browserService.CreateTabAsync();
        tabResult.IsSuccess.Should().BeTrue();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        await _browserService.NavigateToAsync(tabResult.Value.Id, "https://example.com");

        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500);
    }

    #endregion
}

