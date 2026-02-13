// TypeAliases.cs - Backward compatibility type aliases for RetroArch refactoring
// This file ensures that existing code referencing the old model locations continues to work

global using SyncFileInfo = SaveState.Infrastructure.RetroArch.Models.SyncFileInfo;

// Re-export model types for backward compatibility - using explicit type forwarding
namespace SaveState.Core.RetroArch;

// Type aliases for backward compatibility - these point to the new Models namespace
using CoreType = SaveState.Core.RetroArch.Models.CoreType;
using SaveStateFormat = SaveState.Core.RetroArch.Models.SaveStateFormat;
using VideoDriver = SaveState.Core.RetroArch.Models.VideoDriver;
using InputDriver = SaveState.Core.RetroArch.Models.InputDriver;
using CloudSyncProvider = SaveState.Core.RetroArch.Models.CloudSyncProvider;
using InputType = SaveState.Core.RetroArch.Models.InputType;
using ControllerType = SaveState.Core.RetroArch.Models.ControllerType;
using ShaderType = SaveState.Core.RetroArch.Models.ShaderType;
using RetroPadButton = SaveState.Core.RetroArch.Models.RetroPadButton;

// Model class aliases
using RetroArchCoreInfo = SaveState.Core.RetroArch.Models.RetroArchCoreInfo;
using CoreCapabilities = SaveState.Core.RetroArch.Models.CoreCapabilities;
using CoreDownloadInfo = SaveState.Core.RetroArch.Models.CoreDownloadInfo;
using CoreInstallResult = SaveState.Core.RetroArch.Models.CoreInstallResult;
using SaveStateInfo = SaveState.Core.RetroArch.Models.SaveStateInfo;
using SaveStateMetadata = SaveState.Core.RetroArch.Models.SaveStateMetadata;
using SaveStateOptions = SaveState.Core.RetroArch.Models.SaveStateOptions;
using SaveStateResult = SaveState.Core.RetroArch.Models.SaveStateResult;
using RetroArchConfigInfo = SaveState.Core.RetroArch.Models.RetroArchConfigInfo;
using VideoConfig = SaveState.Core.RetroArch.Models.VideoConfig;
using InputConfig = SaveState.Core.RetroArch.Models.InputConfig;
using AudioConfig = SaveState.Core.RetroArch.Models.AudioConfig;
using NetworkConfig = SaveState.Core.RetroArch.Models.NetworkConfig;
using ConfigOption = SaveState.Core.RetroArch.Models.ConfigOption;
using InputDeviceConfig = SaveState.Core.RetroArch.Models.InputDeviceConfig;
using DisplaySettings = SaveState.Core.RetroArch.Models.DisplaySettings;
using ShaderConfig = SaveState.Core.RetroArch.Models.ShaderConfig;
using OverlayConfig = SaveState.Core.RetroArch.Models.OverlayConfig;
using OverlayElement = SaveState.Core.RetroArch.Models.OverlayElement;
using InputMapping = SaveState.Core.RetroArch.Models.InputMapping;
using ControllerConfig = SaveState.Core.RetroArch.Models.ControllerConfig;
using HotkeyConfig = SaveState.Core.RetroArch.Models.HotkeyConfig;
using AnalogStickConfig = SaveState.Core.RetroArch.Models.AnalogStickConfig;
