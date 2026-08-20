using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CofferOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRetirementAccountCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "retirement_accounts",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Currency",
                table: "retirement_accounts");
        }
    }
}
