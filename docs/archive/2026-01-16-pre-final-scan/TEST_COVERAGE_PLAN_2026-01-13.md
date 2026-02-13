# Test Coverage Improvement Plan

Audit of the current test suite revealed significant gaps in coverage, particularly for newer MUGEN features and Machine Learning services. This plan outlines the necessary additions to bring the test suite up to a production-ready standard.

## Proposed Changes

### [MUGEN Infrastructure]

#### [NEW] [MugenFusionServiceTests.cs](file:///c:/Users/Doc/Desktop/SaveStateReborn/tests/SaveState.Infrastructure.Tests/Mugen/MugenFusionServiceTests.cs)

- Unit tests for character fusion logic, ensuring compatibility checks and fusion output are correct.

#### [NEW] [MugenTrainingServiceTests.cs](file:///c:/Users/Doc/Desktop/SaveStateReborn/tests/SaveState.Infrastructure.Tests/Mugen/MugenTrainingServiceTests.cs)

- Tests for training mode automation and data capture.

#### [NEW] [MugenValidationServiceTests.cs](file:///c:/Users/Doc/Desktop/SaveStateReborn/tests/SaveState.Infrastructure.Tests/Mugen/MugenValidationServiceTests.cs)

- Validation logic for character and stage definition files.

### [Machine Learning]

#### [NEW] [MachineLearningServiceTests.cs](file:///c:/Users/Doc/Desktop/SaveStateReborn/tests/SaveState.Infrastructure.Tests/Mugen/MachineLearningServiceTests.cs)

- Tests for `SimpleMachineLearningService` and `AdvancedAiService`, focusing on match prediction accuracy and character balance analysis.

### [Presentation]

#### [NEW] [MachineLearningViewModelTests.cs](file:///c:/Users/Doc/Desktop/SaveStateReborn/tests/SaveState.Presentation.Tests/ViewModels/MachineLearningViewModelTests.cs)

- Unit tests for the ML view model logic, command execution, and property synchronization.

## Verification Plan

### Automated Tests

- Run all new tests using `dotnet test`:

  ```bash
  dotnet test tests/SaveState.Infrastructure.Tests/SaveState.Infrastructure.Tests.csproj
  dotnet test tests/SaveState.Presentation.Tests/SaveState.Presentation.Tests.csproj
  ```

### Manual Verification

- Verify that tests correctly handle edge cases (e.g., missing character files, malformed MUGEN data).
