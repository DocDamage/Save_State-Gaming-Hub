# Implement Trainer Generation

The user wants the system to **persist** the cheats found by the AI into a reusable "Trainer" tool. We will implement a system where the AI can "compile" the found addresses/pointers into a saved JSON file, which the UI then yields as a clickable Trainer dashboard.

## Proposed Changes

### Core Services
#### [NEW] [TrainerService.cs](file:///c:/Users/Doc/Desktop/SaveState2/src/SaveState.Core/Services/TrainerService.cs)
- Manage `TrainerDefinition` entities.
- Methods: `CreateTrainer(gameName)`, `AddCheat(gameName, cheatName, address, type, value)`, `LoadTrainers()`.
- Save format: `ProcessName.json` containing a list of cheats.

#### [UPDATE] [CheatAgentService.cs](file:///c:/Users/Doc/Desktop/SaveState2/src/SaveState.Core/Services/CheatAgentService.cs)
- Add `CREATE_CHEAT: <name> <address/pointer> <type> <value>` tool.
- Instruct AI to use this when user says "Make a trainer".

### UI Layer
#### [NEW] [TrainerView.axaml](file:///c:/Users/Doc/Desktop/SaveState2/src/SaveState.UI/Views/TrainerView.axaml)
- A list of detected trainers (based on JSON files).
- When a game is selected, show list of Cheats.
- **Toggle Switches** for freezing values.
- **Input Fields** for writing values.

### Verification
1.  **Chat**: "Find pointer for Health... Make a trainer called God Mode".
2.  **Result**: AI confirms "Created 'God Mode' cheat for FFVI".
3.  **UI**: User goes to "Trainers" tab, sees "God Mode" toggle.
