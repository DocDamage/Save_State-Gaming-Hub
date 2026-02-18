// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveState.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSmartLauncher : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LaunchProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    GameId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    DisableGameMode = table.Column<bool>(type: "INTEGER", nullable: false),
                    DisableFullscreenOptimizations = table.Column<bool>(type: "INTEGER", nullable: false),
                    RunAsAdministrator = table.Column<bool>(type: "INTEGER", nullable: false),
                    DisableWindowsDefender = table.Column<bool>(type: "INTEGER", nullable: false),
                    ProcessesToSuspend = table.Column<string>(type: "TEXT", nullable: false),
                    ServicesToStop = table.Column<string>(type: "TEXT", nullable: false),
                    PowerPlanGuid = table.Column<string>(type: "TEXT", nullable: true),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EstimatedPerformanceGain = table.Column<int>(type: "INTEGER", nullable: true),
                    DisplaySettings_ResolutionWidth = table.Column<int>(type: "INTEGER", nullable: true),
                    DisplaySettings_ResolutionHeight = table.Column<int>(type: "INTEGER", nullable: true),
                    DisplaySettings_RefreshRate = table.Column<int>(type: "INTEGER", nullable: true),
                    DisplaySettings_EnableHDR = table.Column<bool>(type: "INTEGER", nullable: true),
                    DisplaySettings_DisableFullscreenOptimizations = table.Column<bool>(type: "INTEGER", nullable: false),
                    DisplaySettings_OverrideDPIScaling = table.Column<bool>(type: "INTEGER", nullable: true),
                    PerformanceSettings_EnableMemoryOptimization = table.Column<bool>(type: "INTEGER", nullable: false),
                    PerformanceSettings_EnableCPUParking = table.Column<bool>(type: "INTEGER", nullable: false),
                    PerformanceSettings_DisableVisualEffects = table.Column<bool>(type: "INTEGER", nullable: false),
                    PerformanceSettings_ClearStandbyList = table.Column<bool>(type: "INTEGER", nullable: false),
                    PerformanceSettings_TargetFPS = table.Column<int>(type: "INTEGER", nullable: true),
                    PerformanceSettings_EnableHardwareGPUScheduling = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LaunchProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LaunchSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GameId = table.Column<Guid>(type: "TEXT", nullable: false),
                    GameName = table.Column<string>(type: "TEXT", nullable: false),
                    ProfileId = table.Column<Guid>(type: "TEXT", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ExitCode = table.Column<int>(type: "INTEGER", nullable: true),
                    InitialSystemState_PowerPlanGuid = table.Column<string>(type: "TEXT", nullable: true),
                    InitialSystemState_VisualEffectsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    PerformanceMetrics_AverageFPS = table.Column<double>(type: "REAL", nullable: true),
                    PerformanceMetrics_MinFPS = table.Column<double>(type: "REAL", nullable: true),
                    PerformanceMetrics_MaxFPS = table.Column<double>(type: "REAL", nullable: true),
                    PerformanceMetrics_AverageCPUUsage = table.Column<double>(type: "REAL", nullable: true),
                    PerformanceMetrics_AverageGPUUsage = table.Column<double>(type: "REAL", nullable: true),
                    PerformanceMetrics_PeakMemoryMB = table.Column<long>(type: "INTEGER", nullable: true),
                    PerformanceMetrics_AverageTemperature = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LaunchSessions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LaunchProfiles_GameId_IsDefault",
                table: "LaunchProfiles",
                columns: new[] { "GameId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_LaunchProfiles_IsActive",
                table: "LaunchProfiles",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_LaunchSessions_GameId_EndedAt",
                table: "LaunchSessions",
                columns: new[] { "GameId", "EndedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LaunchSessions_StartedAt",
                table: "LaunchSessions",
                column: "StartedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LaunchProfiles");

            migrationBuilder.DropTable(
                name: "LaunchSessions");
        }
    }
}
