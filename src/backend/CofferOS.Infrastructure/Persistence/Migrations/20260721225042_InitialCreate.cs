using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CofferOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "labels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    WalletId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Target = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Reference = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Text = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_labels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "notes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    WalletId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Target = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Reference = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "wallets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Network = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    WatchOnly = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wallets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "descriptors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    WalletId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    ScriptType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Raw = table.Column<string>(type: "TEXT", nullable: false),
                    MasterFingerprint = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    DerivationPath = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Checksum = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    Threshold = table.Column<int>(type: "INTEGER", nullable: true),
                    IsSortedMulti = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_descriptors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_descriptors_wallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    WalletId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TxId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    NetAmountSats = table.Column<long>(type: "INTEGER", nullable: false),
                    FeeSats = table.Column<long>(type: "INTEGER", nullable: false),
                    Direction = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Confirmations = table.Column<int>(type: "INTEGER", nullable: false),
                    BlockHeight = table.Column<long>(type: "INTEGER", nullable: true),
                    BlockHash = table.Column<string>(type: "TEXT", nullable: true),
                    Timestamp = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_transactions_wallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "utxos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    WalletId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TxId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Vout = table.Column<int>(type: "INTEGER", nullable: false),
                    ValueSats = table.Column<long>(type: "INTEGER", nullable: false),
                    ScriptPubKeyHex = table.Column<string>(type: "TEXT", nullable: false),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    Confirmations = table.Column<int>(type: "INTEGER", nullable: false),
                    BlockHeight = table.Column<long>(type: "INTEGER", nullable: true),
                    IsSpent = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_utxos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_utxos_wallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "addresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    WalletId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DescriptorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DerivationIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    IsChange = table.Column<bool>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    ScriptPubKeyHex = table.Column<string>(type: "TEXT", nullable: false),
                    IsUsed = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_addresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_addresses_descriptors_DescriptorId",
                        column: x => x.DescriptorId,
                        principalTable: "descriptors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cosigners",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrderIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    MasterFingerprint = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    OriginPath = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    KeyExpression = table.Column<string>(type: "TEXT", nullable: false),
                    DescriptorId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cosigners", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cosigners_descriptors_DescriptorId",
                        column: x => x.DescriptorId,
                        principalTable: "descriptors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_addresses_DescriptorId",
                table: "addresses",
                column: "DescriptorId");

            migrationBuilder.CreateIndex(
                name: "IX_addresses_Value",
                table: "addresses",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_addresses_WalletId",
                table: "addresses",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "IX_cosigners_DescriptorId",
                table: "cosigners",
                column: "DescriptorId");

            migrationBuilder.CreateIndex(
                name: "IX_descriptors_WalletId",
                table: "descriptors",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "IX_labels_WalletId",
                table: "labels",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "IX_notes_WalletId",
                table: "notes",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_WalletId",
                table: "transactions",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_WalletId_TxId",
                table: "transactions",
                columns: new[] { "WalletId", "TxId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_utxos_WalletId",
                table: "utxos",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "IX_utxos_WalletId_TxId_Vout",
                table: "utxos",
                columns: new[] { "WalletId", "TxId", "Vout" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "addresses");

            migrationBuilder.DropTable(
                name: "cosigners");

            migrationBuilder.DropTable(
                name: "labels");

            migrationBuilder.DropTable(
                name: "notes");

            migrationBuilder.DropTable(
                name: "transactions");

            migrationBuilder.DropTable(
                name: "utxos");

            migrationBuilder.DropTable(
                name: "descriptors");

            migrationBuilder.DropTable(
                name: "wallets");
        }
    }
}
