// SpectatorModeService has been refactored into a modular architecture.
// All types have been moved to the SpectatorModeService/ directory.
//
// New structure:
// - SpectatorModeService/SpectatorModeServiceCoordinator.cs - Main service implementation
// - SpectatorModeService/ISpectatorModeService.cs - Service interface
// - SpectatorModeService/Models/ - All data models
// - SpectatorModeService/Engines/ - Specialized engines:
//   * SessionEngine - Session lifecycle and viewer tracking
//   * CameraEngine - Camera angle management
//   * OverlayEngine - UI overlay management
//   * ChatEngine - Chat message handling
//   * HighlightEngine - Match highlights and replays
// - SpectatorModeService/TypeAliases.cs - Backward compatibility types

// Re-export all types for backward compatibility
global using SpectatorSession = SaveState.Application.Mugen.Services.SpectatorSession;
global using SpectatorControl = SaveState.Application.Mugen.Services.SpectatorControl;
global using MatchSpectatorData = SaveState.Application.Mugen.Services.MatchSpectatorData;
global using ChatMessage = SaveState.Application.Mugen.Services.ChatMessage;
global using MatchStatistics = SaveState.Application.Mugen.Services.MatchStatistics;
global using MatchHighlights = SaveState.Application.Mugen.Services.MatchHighlights;
global using SpectatorHighlightMoment = SaveState.Application.Mugen.Services.SpectatorHighlightMoment;
global using ReplayRequest = SaveState.Application.Mugen.Services.ReplayRequest;

namespace SaveState.Application.Mugen.Services;

// This file is now a compatibility shim.
// The actual implementation is in SpectatorModeService/SpectatorModeServiceCoordinator.cs
