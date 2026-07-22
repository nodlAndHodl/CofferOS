using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CofferOS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CofferOSDbContext))]
[Migration("20260723010000_AddAddressState")]
public partial class AddAddressState : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "UseCount",
            table: "addresses",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<string>(
            name: "FirstTxId",
            table: "addresses",
            type: "TEXT",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "LastTxId",
            table: "addresses",
            type: "TEXT",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<long>(
            name: "CurrentSats",
            table: "addresses",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0L);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CurrentSats",
            table: "addresses");

        migrationBuilder.DropColumn(
            name: "LastTxId",
            table: "addresses");

        migrationBuilder.DropColumn(
            name: "FirstTxId",
            table: "addresses");

        migrationBuilder.DropColumn(
            name: "UseCount",
            table: "addresses");
    }
}
