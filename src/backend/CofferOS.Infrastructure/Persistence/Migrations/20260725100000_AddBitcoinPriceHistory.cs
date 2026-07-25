using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CofferOS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CofferOSDbContext))]
[Migration("20260725100000_AddBitcoinPriceHistory")]
public partial class AddBitcoinPriceHistory : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "bitcoin_price_history",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Timestamp = table.Column<long>(type: "INTEGER", nullable: false),
                PriceUsd = table.Column<decimal>(type: "TEXT", nullable: false),
                Provider = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_bitcoin_price_history", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_bitcoin_price_history_Timestamp",
            table: "bitcoin_price_history",
            column: "Timestamp");

        migrationBuilder.CreateIndex(
            name: "IX_bitcoin_price_history_Provider",
            table: "bitcoin_price_history",
            column: "Provider");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "bitcoin_price_history");
    }
}
