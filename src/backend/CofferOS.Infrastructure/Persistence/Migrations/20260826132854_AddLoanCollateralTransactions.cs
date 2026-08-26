using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CofferOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLoanCollateralTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "loan_collateral_transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LoanId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TransactionType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    AmountBtc = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    BtcPriceAtTime = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    CollateralAmountBtcBefore = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    CollateralAmountBtcAfter = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    LtvAtTime = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    TransactionDate = table.Column<long>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loan_collateral_transactions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_loan_collateral_transactions_LoanId",
                table: "loan_collateral_transactions",
                column: "LoanId");

            migrationBuilder.CreateIndex(
                name: "IX_loan_collateral_transactions_LoanId_TransactionDate",
                table: "loan_collateral_transactions",
                columns: new[] { "LoanId", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_loan_collateral_transactions_TransactionDate",
                table: "loan_collateral_transactions",
                column: "TransactionDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "loan_collateral_transactions");
        }
    }
}
