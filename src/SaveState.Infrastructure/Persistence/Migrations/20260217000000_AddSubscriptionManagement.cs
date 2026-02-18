// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveState.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServiceId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ServiceName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SubscriptionType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Tier = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    MonthlyPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    AutoRenew = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSubscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrackedSubscriptionGames",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    GameTitle = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    PreferredServiceId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    TrackedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    NotifyOnAvailable = table.Column<bool>(type: "INTEGER", nullable: false),
                    NotifyOnLeaving = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackedSubscriptionGames", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionCatalogCache",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServiceId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    GameTitle = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Developer = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Publisher = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DateLeaving = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Genres = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    MetacriticScore = table.Column<int>(type: "INTEGER", nullable: true),
                    CachedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionCatalogCache", x => x.Id);
                });

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_UserId_ServiceId",
                table: "UserSubscriptions",
                columns: new[] { "UserId", "ServiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_UserId",
                table: "UserSubscriptions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_IsActive",
                table: "UserSubscriptions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TrackedSubscriptionGames_UserId_GameTitle",
                table: "TrackedSubscriptionGames",
                columns: new[] { "UserId", "GameTitle" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionCatalogCache_ServiceId_GameTitle",
                table: "SubscriptionCatalogCache",
                columns: new[] { "ServiceId", "GameTitle" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionCatalogCache_DateLeaving",
                table: "SubscriptionCatalogCache",
                column: "DateLeaving");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSubscriptions");

            migrationBuilder.DropTable(
                name: "TrackedSubscriptionGames");

            migrationBuilder.DropTable(
                name: "SubscriptionCatalogCache");
        }
    }
}
