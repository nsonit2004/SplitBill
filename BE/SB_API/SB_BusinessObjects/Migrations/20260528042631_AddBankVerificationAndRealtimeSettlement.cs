using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SB_BusinessObjects.Migrations
{
    /// <inheritdoc />
    public partial class AddBankVerificationAndRealtimeSettlement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "BankAccountVerified",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "BankAccountVerifiedAt",
                table: "Users",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankVerificationProvider",
                table: "Users",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "BankVerifiedAt",
                table: "SettleTransactions",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransferReference",
                table: "SettleTransactions",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_SettleTransactions_TransferReference",
                table: "SettleTransactions",
                column: "TransferReference",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SettleTransactions_TransferReference",
                table: "SettleTransactions");

            migrationBuilder.DropColumn(
                name: "BankAccountVerified",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BankAccountVerifiedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BankVerificationProvider",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BankVerifiedAt",
                table: "SettleTransactions");

            migrationBuilder.DropColumn(
                name: "TransferReference",
                table: "SettleTransactions");
        }
    }
}
