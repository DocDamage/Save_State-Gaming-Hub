// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveState.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGameDeals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GameDeals",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    TitlePlain = table.Column<string>(type: "TEXT", nullable: true),
                    ImageUrl = table.Column<string>(type: "TEXT", nullable: true),
                    CurrentPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RegularPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    StoreId = table.Column<string>(type: "TEXT", nullable: false),
                    StoreName = table.Column<string>(type: "TEXT", nullable: false),
                    StoreColor = table.Column<string>(type: "TEXT", nullable: false),
                    StoreIsOfficial = table.Column<bool>(type: "INTEGER", nullable: false),
                    DealStart = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DealEnd = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsHistoricalLow = table.Column<bool>(type: "INTEGER", nullable: false),
                    StoreUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Drm = table.Column<string>(type: "TEXT", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MetacriticScore = table.Column<int>(type: "INTEGER", nullable: true),
                    SteamRating = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameDeals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PriceHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GameTitle = table.Column<string>(type: "TEXT", nullable: false),
                    StoreId = table.Column<string>(type: "TEXT", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    WasOnSale = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PriceAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    GameTitle = table.Column<string>(type: "TEXT", nullable: false),
                    TargetPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TargetDiscountPercent = table.Column<decimal>(type: "TEXT", nullable: true),
                    StoreIds = table.Column<string>(type: "TEXT", nullable: false),
                    AlertOnHistoricalLow = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastTriggeredAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MinHoursBetweenAlerts = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceAlerts", x => x.Id);
                });

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_GameDeals_Title",
                table: "GameDeals",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_GameDeals_CurrentPrice",
                table: "GameDeals",
                column: "CurrentPrice");

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistory_GameTitle_StoreId",
                table: "PriceHistory",
                columns: new[] { "GameTitle", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistory_Date",
                table: "PriceHistory",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_PriceAlerts_UserId",
                table: "PriceAlerts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceAlerts_IsActive_GameTitle",
                table: "PriceAlerts",
                columns: new[] { "IsActive", "GameTitle" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameDeals");

            migrationBuilder.DropTable(
                name: "PriceHistory");

            migrationBuilder.DropTable(
                name: "PriceAlerts");
        }
    }
}
