
## Phase 6: Code Quality & Warning Reduction

**Objective**: Systematically reduce compilation warnings to improve code quality and maintainability.
**Target Warnings**: CS8618 (Non-nullable property), CS1998 (Async lacks await), CS1591 (Xml Comments).
**Target Count**: < 50 warnings (from ~447).

### 6.1 Critical Warning Fixes (Compliance)

- [x] **CS8618: Non-nullable property uninitialized**
  - [x] `SaveState.Core` Entities (BoundedContexts, ValueObjects)
  - [x] `SaveState.Application` DTOs and Commands (Required Properties)
  - [x] Result: 0 CS8618 warnings remaining.

- [ ] **CS1998: Async method lacks await**
  - [x] `SaveState.Infrastructure` (BackupScheduler, MacroManager, SpeechRecognition)
  - [ ] `SaveState.Presentation` (ViewModels)
  - [ ] Test Projects (Low Priority)

- [ ] **CS1591: Missing XML Comments**
  - [ ] Public APIs in `Core` and `Application`
