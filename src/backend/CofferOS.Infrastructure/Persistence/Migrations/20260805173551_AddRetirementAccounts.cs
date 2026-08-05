using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CofferOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRetirementAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "retirement_accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    AccountType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    BitcoinAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_retirement_accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "retirement_account_cost_basis",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CostBasis = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    AcquisitionDate = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_retirement_account_cost_basis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_retirement_account_cost_basis_retirement_accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "retirement_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_retirement_account_cost_basis_AccountId",
                table: "retirement_account_cost_basis",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_retirement_accounts_CreatedAt",
                table: "retirement_accounts",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "retirement_account_cost_basis");

            migrationBuilder.DropTable(
                name: "retirement_accounts");
        }
    }
}
