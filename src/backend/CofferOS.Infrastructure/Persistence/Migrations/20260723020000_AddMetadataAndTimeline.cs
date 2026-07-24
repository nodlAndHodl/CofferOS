using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CofferOS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CofferOSDbContext))]
[Migration("20260723020000_AddMetadataAndTimeline")]
public partial class AddMetadataAndTimeline : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "tags",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                WalletId = table.Column<Guid>(type: "TEXT", nullable: false),
                Target = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                Reference = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Value = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_tags", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "categories",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                WalletId = table.Column<Guid>(type: "TEXT", nullable: false),
                Target = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                Reference = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_categories", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "metadata_entries",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                WalletId = table.Column<Guid>(type: "TEXT", nullable: false),
                Target = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                Reference = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Key = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Value = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_metadata_entries", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "timeline_events",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                WalletId = table.Column<Guid>(type: "TEXT", nullable: false),
                Type = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                OccurredAt = table.Column<long>(type: "INTEGER", nullable: false),
                Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                Reference = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_timeline_events", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_tags_WalletId",
            table: "tags",
            column: "WalletId");

        migrationBuilder.CreateIndex(
            name: "IX_tags_WalletId_Target_Reference_Value",
            table: "tags",
            columns: new[] { "WalletId", "Target", "Reference", "Value" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_categories_WalletId",
            table: "categories",
            column: "WalletId");

        migrationBuilder.CreateIndex(
            name: "IX_categories_WalletId_Target_Reference",
            table: "categories",
            columns: new[] { "WalletId", "Target", "Reference" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_metadata_entries_WalletId",
            table: "metadata_entries",
            column: "WalletId");

        migrationBuilder.CreateIndex(
            name: "IX_metadata_entries_WalletId_Target_Reference_Key",
            table: "metadata_entries",
            columns: new[] { "WalletId", "Target", "Reference", "Key" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_timeline_events_WalletId",
            table: "timeline_events",
            column: "WalletId");

        migrationBuilder.CreateIndex(
            name: "IX_timeline_events_WalletId_OccurredAt",
            table: "timeline_events",
            columns: new[] { "WalletId", "OccurredAt" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "timeline_events");
        migrationBuilder.DropTable(name: "metadata_entries");
        migrationBuilder.DropTable(name: "categories");
        migrationBuilder.DropTable(name: "tags");
    }
}
