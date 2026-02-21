# 🔌 Dialog Integration Guide

**Date**: January 4, 2026
**Status**: Integration Instructions
**Target ViewModels**: GameNotesTabViewModel, GameOverviewTabViewModel, GameModsTabViewModel

---

## ✅ Step 1: DI Registration (COMPLETE)

The `IDialogService` has been registered in `Program.cs`:

```csharp
builder.Services.AddSingleton<IDialogService, DialogService>();
```

---

## 📝 Step 2: GameNotesTabViewModel Integration

### Required Changes

#### 1. Add Using Statement

```csharp
using SaveState.Presentation.Services;
```

#### 2. Add Fields

```csharp
private readonly IDialogService _dialogService;
private GameId? _currentGameId;
```

#### 3. Update Constructor

```csharp
public GameNotesTabViewModel(
    IMediator mediator,
    IUserContextService userContextService,
    IDialogService dialogService, // ADD THIS
    ILogger<GameNotesTabViewModel> logger)
{
    _mediator = mediator;
    _userContextService = userContextService;
    _dialogService = dialogService; // ADD THIS
    _logger = logger;
}
```

#### 4. Store GameId in LoadDataAsync

```csharp
public async Task LoadDataAsync(GameId gameId)
{
    _currentGameId = gameId; // ADD THIS LINE
    try
    {
        // ... rest of method
    }
}
```

#### 5. Update AddNote Command

Replace the existing `AddNote` method with:

```csharp
[RelayCommand]
private async Task AddNote()
{
    if (_currentGameId == null)
    {
        _logger.LogWarning("Cannot add note - no game ID");
        return;
    }

    var result = await _dialogService.ShowNoteEditorAsync();
    if (result != null)
    {
        try
        {
            // Create note via MediatR command
            var command = new CreateGameNoteCommand
            {
                GameId = _currentGameId.Value.Value,
                UserId = _userContextService.GetCurrentUserId()!.Value,
                Title = result.Title,
                Content = result.Content,
                Category = result.Category,
                IsPinned = result.IsPinned
            };

            await _mediator.Send(command);
            _logger.LogInformation("Note created: {Title}", result.Title);

            // Reload notes
            await LoadDataAsync(_currentGameId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create note");
            await _dialogService.ShowErrorAsync("Error", "Failed to create note. Please try again.");
        }
    }
}
```

#### 6. Update EditNote Command in GameNoteViewModel

Replace the existing `EditNote` method with:

```csharp
[RelayCommand]
private async Task EditNote()
{
    // TODO: Edit this note
    _logger.LogInformation("Edit note requested for note {NoteId}", Id);
}
```

---

## 🏷️ Step 3: GameOverviewTabViewModel Integration

### Required Changes

#### 1. Add Using Statement

```csharp
using SaveState.Presentation.Services;
```

#### 2. Add Field

```csharp
private readonly IDialogService _dialogService;
private GameId? _currentGameId;
```

#### 3. Update Constructor

```csharp
public GameOverviewTabViewModel(
    IMediator mediator,
    IUserContextService userContextService,
    IAiOrchestrator aiOrchestrator,
    IDialogService dialogService, // ADD THIS
    ILogger<GameOverviewTabViewModel> logger)
{
    _mediator = mediator;
    _userContextService = userContextService;
    _aiOrchestrator = aiOrchestrator;
    _dialogService = dialogService; // ADD THIS
    _logger = logger;
}
```

#### 4. Store GameId in LoadDataAsync

```csharp
public async Task LoadDataAsync(GameId gameId)
{
    _currentGameId = gameId; // ADD THIS LINE
    try
    {
        // ... rest of method
    }
}
```

#### 5. Update EditTags Command

```csharp
[RelayCommand]
private async Task EditTags()
{
    if (_currentGameId == null) return;

    var currentTags = GameTags.ToArray();
    var result = await _dialogService.ShowTagEditorAsync(currentTags);

    if (result != null)
    {
        try
        {
            // Update tags via MediatR command
            var command = new UpdateGameTagsCommand
            {
                GameId = _currentGameId.Value.Value,
                Tags = result.Tags
            };

            await _mediator.Send(command);
            _logger.LogInformation("Tags updated for game {GameId}", _currentGameId);

            // Update UI
            GameTags.Clear();
            foreach (var tag in result.Tags)
            {
                GameTags.Add(tag);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update tags");
            await _dialogService.ShowErrorAsync("Error", "Failed to update tags. Please try again.");
        }
    }
}
```

#### 6. Update AddGoal Command

```csharp
[RelayCommand]
private async Task AddGoal()
{
    if (_currentGameId == null) return;

    var result = await _dialogService.ShowGoalCreationDialogAsync();

    if (result != null)
    {
        try
        {
            // Create goal via MediatR command
            var command = new CreateGameGoalCommand
            {
                GameId = _currentGameId.Value.Value,
                UserId = _userContextService.GetCurrentUserId()!.Value,
                Title = result.Title,
                Description = result.Description,
                TargetDate = result.TargetDate,
                GoalType = result.GoalType
            };

            await _mediator.Send(command);
            _logger.LogInformation("Goal created: {Title}", result.Title);

            await _dialogService.ShowInformationAsync("Success", "Goal created successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create goal");
            await _dialogService.ShowErrorAsync("Error", "Failed to create goal. Please try again.");
        }
    }
}
```

#### 7. Update WriteReview Command

```csharp
[RelayCommand]
private async Task WriteReview()
{
    if (_currentGameId == null) return;

    var result = await _dialogService.ShowReviewEditorAsync();

    if (result != null)
    {
        try
        {
            // Save review via MediatR command
            var command = new CreateGameReviewCommand
            {
                GameId = _currentGameId.Value.Value,
                UserId = _userContextService.GetCurrentUserId()!.Value,
                ReviewText = result.ReviewText,
                Rating = result.Rating,
                RecommendToFriends = result.RecommendToFriends
            };

            await _mediator.Send(command);
            _logger.LogInformation("Review saved for game {GameId}", _currentGameId);

            await _dialogService.ShowInformationAsync("Success", "Review saved successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save review");
            await _dialogService.ShowErrorAsync("Error", "Failed to save review. Please try again.");
        }
    }
}
```

---

## 🎮 Step 4: GameModsTabViewModel Integration

### Required Changes

#### 1. Add Using Statement

```csharp
using SaveState.Presentation.Services;
```

#### 2. Add Field

```csharp
private readonly IDialogService _dialogService;
```

#### 3. Update Constructor

```csharp
public GameModsTabViewModel(
    IMediator mediator,
    IModManagementService modService,
    INotificationService notificationService,
    IDialogService dialogService, // ADD THIS
    ILogger<GameModsTabViewModel> logger)
{
    _mediator = mediator;
    _modService = modService;
    _notificationService = notificationService;
    _dialogService = dialogService; // ADD THIS
    _logger = logger;
}
```

#### 4. Update InstallMod Command

Replace the existing method with:

```csharp
[RelayCommand]
private async Task InstallMod()
{
    if (_gameId == null)
    {
        _logger.LogWarning("Cannot install mod - no game ID");
        return;
    }

    var files = await _dialogService.ShowModFilePickerAsync();
    if (files != null && files.Length > 0)
    {
        try
        {
            foreach (var filePath in files)
            {
                var result = await _modService.InstallModAsync(_gameId, filePath);

                if (result.IsSuccess)
                {
                    _notificationService.ShowSuccess("Mod Installed", $"Successfully installed mod from {Path.GetFileName(filePath)}");
                }
                else
                {
                    _notificationService.ShowError("Installation Failed", result.Error ?? "Unknown error");
                }
            }

            // Refresh mod list
            await RefreshMods();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install mod");
            await _dialogService.ShowErrorAsync("Error", "Failed to install mod. Please try again.");
        }
    }
}
```

---

## 🧪 Step 5: Testing

### Manual Testing Checklist

1. **Note Editor**
   - [ ] Click "Add Note" button
   - [ ] Dialog appears
   - [ ] Enter title and content
   - [ ] Click "Save Note"
   - [ ] Note appears in list
   - [ ] Click "Cancel" closes dialog without saving

2. **Tag Editor**
   - [ ] Click "Edit Tags" button
   - [ ] Dialog appears with current tags
   - [ ] Add new tag
   - [ ] Remove existing tag
   - [ ] Click suggested tag
   - [ ] Click "Save Tags"
   - [ ] Tags update in UI

3. **Goal Creation**
   - [ ] Click "Add Goal" button
   - [ ] Dialog appears
   - [ ] Fill in goal details
   - [ ] Select goal type
   - [ ] Set target date
   - [ ] Click "Create Goal"
   - [ ] Success message appears

4. **Review Editor**
   - [ ] Click "Write Review" button
   - [ ] Dialog appears
   - [ ] Click stars to rate
   - [ ] Write review text
   - [ ] Toggle recommendation
   - [ ] Click "Save Review"
   - [ ] Success message appears

5. **Mod File Picker**
   - [ ] Click "Install Mod" button
   - [ ] File picker appears
   - [ ] Select mod file(s)
   - [ ] Files are processed
   - [ ] Success notification appears

---

## ⚠️ Important Notes

### Missing Commands

Some commands referenced in the integration don't exist yet in the Application layer:

- `CreateGameNoteCommand`
- `UpdateGameTagsCommand`
- `CreateGameGoalCommand`
- `CreateGameReviewCommand`

These need to be created in `SaveState.Application` before the full integration will work.

### Alternative Approach

Until the commands are created, you can:

1. Show the dialogs and log the results
2. Display success messages
3. Update the UI optimistically

Example:

```csharp
var result = await _dialogService.ShowNoteEditorAsync();
if (result != null)
{
    _logger.LogInformation("Note would be created: {Title}", result.Title);
    await _dialogService.ShowInformationAsync("Success", "Note saved! (Demo mode)");
}
```

---

## 📊 Integration Progress

| ViewModel | Dialog Service | Commands Updated | Status |
|-----------|---------------|------------------|--------|
| GameNotesTabViewModel | ✅ Injected | AddNote | 🟡 Partial |
| GameOverviewTabViewModel | ✅ Injected | EditTags, AddGoal, WriteReview | 🟡 Partial |
| GameModsTabViewModel | ✅ Injected | InstallMod | 🟡 Partial |

**Status Legend**:

- ✅ Complete
- 🟡 Partial (needs backend commands)
- ⏳ In Progress
- ❌ Not Started

---

## 🚀 Next Steps

1. **Create Missing Commands** (Application layer)
   - CreateGameNoteCommand
   - UpdateGameTagsCommand
   - CreateGameGoalCommand
   - CreateGameReviewCommand

2. **Update ViewModels** (Follow this guide)
   - GameNotesTabViewModel
   - GameOverviewTabViewModel
   - GameModsTabViewModel

3. **Test End-to-End**
   - Manual testing
   - Verify data persistence
   - Check error handling

4. **Polish**
   - Add loading indicators
   - Improve error messages
   - Add confirmation dialogs for destructive actions

---

**Last Updated**: January 4, 2026, 2:15 AM
**Status**: Ready for implementation
**Estimated Time**: 30-45 minutes for full integration
