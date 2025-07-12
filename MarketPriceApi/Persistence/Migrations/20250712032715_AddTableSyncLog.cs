using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketPriceApi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTableSyncLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Price",
                table: "AssetPrices",
                newName: "Volume");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Assets",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "Exchange",
                table: "Assets",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Assets",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "Assets",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncAt",
                table: "Assets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Provider",
                table: "Assets",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Close",
                table: "AssetPrices",
                type: "numeric(18,6)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "High",
                table: "AssetPrices",
                type: "numeric(18,6)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Interval",
                table: "AssetPrices",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Low",
                table: "AssetPrices",
                type: "numeric(18,6)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Open",
                table: "AssetPrices",
                type: "numeric(18,6)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Provider",
                table: "AssetPrices",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "SyncLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Operation = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsSuccess = table.Column<bool>(type: "boolean", nullable: false),
                    RecordsProcessed = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    InstrumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Symbol = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyncLogs_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assets_Provider_Kind",
                table: "Assets",
                columns: new[] { "Provider", "Kind" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetPrices_AssetId_Interval_Timestamp",
                table: "AssetPrices",
                columns: new[] { "AssetId", "Interval", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetPrices_Timestamp",
                table: "AssetPrices",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_SyncLogs_AssetId",
                table: "SyncLogs",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_SyncLogs_IsSuccess",
                table: "SyncLogs",
                column: "IsSuccess");

            migrationBuilder.CreateIndex(
                name: "IX_SyncLogs_Operation_StartedAt",
                table: "SyncLogs",
                columns: new[] { "Operation", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncLogs_Symbol",
                table: "SyncLogs",
                column: "Symbol");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncLogs");

            migrationBuilder.DropIndex(
                name: "IX_Assets_Provider_Kind",
                table: "Assets");

            migrationBuilder.DropIndex(
                name: "IX_AssetPrices_AssetId_Interval_Timestamp",
                table: "AssetPrices");

            migrationBuilder.DropIndex(
                name: "IX_AssetPrices_Timestamp",
                table: "AssetPrices");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "Exchange",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "LastSyncAt",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "Close",
                table: "AssetPrices");

            migrationBuilder.DropColumn(
                name: "High",
                table: "AssetPrices");

            migrationBuilder.DropColumn(
                name: "Interval",
                table: "AssetPrices");

            migrationBuilder.DropColumn(
                name: "Low",
                table: "AssetPrices");

            migrationBuilder.DropColumn(
                name: "Open",
                table: "AssetPrices");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "AssetPrices");

            migrationBuilder.RenameColumn(
                name: "Volume",
                table: "AssetPrices",
                newName: "Price");
        }
    }
}
