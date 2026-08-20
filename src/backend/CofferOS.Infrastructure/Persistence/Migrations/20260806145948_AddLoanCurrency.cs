using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CofferOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLoanCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "loans",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "user_settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SettingsJson = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_settings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_settings");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "loans");
        }
    }
}
