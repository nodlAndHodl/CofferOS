using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CofferOS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CofferOSDbContext))]
[Migration("20260725083300_AddLoans")]
public partial class AddLoans : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "loans",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Lender = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                Notes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                PrincipalAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                CurrentBalance = table.Column<decimal>(type: "TEXT", nullable: false),
                InterestRate = table.Column<decimal>(type: "TEXT", nullable: false),
                InterestType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                LoanStartDate = table.Column<long>(type: "INTEGER", nullable: false),
                LoanTermMonths = table.Column<int>(type: "INTEGER", nullable: true),
                PaymentFrequency = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                CollateralAmountBtc = table.Column<decimal>(type: "TEXT", nullable: false),
                CurrentBtcPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                WarningLtv = table.Column<decimal>(type: "TEXT", nullable: false),
                LiquidationLtv = table.Column<decimal>(type: "TEXT", nullable: false),
                CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_loans", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_loans_Status",
            table: "loans",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_loans_CreatedAt",
            table: "loans",
            column: "CreatedAt");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "loans");
    }
}
