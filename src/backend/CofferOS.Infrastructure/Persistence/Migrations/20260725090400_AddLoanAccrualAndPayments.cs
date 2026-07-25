using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CofferOS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CofferOSDbContext))]
[Migration("20260725090400_AddLoanAccrualAndPayments")]
public partial class AddLoanAccrualAndPayments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "AccruedInterest",
            table: "loans",
            type: "TEXT",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<long>(
            name: "LastAccruedOn",
            table: "loans",
            type: "INTEGER",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "loan_payments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                LoanId = table.Column<Guid>(type: "TEXT", nullable: false),
                PaymentDate = table.Column<long>(type: "INTEGER", nullable: false),
                TotalAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                PrincipalAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                InterestAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_loan_payments", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_loan_payments_LoanId",
            table: "loan_payments",
            column: "LoanId");

        migrationBuilder.CreateIndex(
            name: "IX_loan_payments_PaymentDate",
            table: "loan_payments",
            column: "PaymentDate");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "loan_payments");

        migrationBuilder.DropColumn(name: "LastAccruedOn", table: "loans");
        migrationBuilder.DropColumn(name: "AccruedInterest", table: "loans");
    }
}
