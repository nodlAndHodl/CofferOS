using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CofferOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLoanPriceSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "BalanceOverridden",
                table: "loans",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "loan_price_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LoanId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SnapshotDate = table.Column<long>(type: "INTEGER", nullable: false),
                    PriceUsd = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loan_price_snapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_loan_price_snapshots_LoanId",
                table: "loan_price_snapshots",
                column: "LoanId");

            migrationBuilder.CreateIndex(
                name: "IX_loan_price_snapshots_LoanId_SnapshotDate",
                table: "loan_price_snapshots",
                columns: new[] { "LoanId", "SnapshotDate" });

            migrationBuilder.CreateIndex(
                name: "IX_loan_price_snapshots_SnapshotDate",
                table: "loan_price_snapshots",
                column: "SnapshotDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "loan_price_snapshots");

            migrationBuilder.DropColumn(
                name: "BalanceOverridden",
                table: "loans");
        }
    }
}
