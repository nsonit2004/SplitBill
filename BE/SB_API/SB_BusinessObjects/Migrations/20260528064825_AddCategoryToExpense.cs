using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SB_BusinessObjects.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryToExpense : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Expenses",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            // Backfill NULL categories to 'Other' for existing records
            migrationBuilder.Sql("UPDATE `Expenses` SET `Category` = 'Other' WHERE `Category` IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Expenses");
        }
    }
}
