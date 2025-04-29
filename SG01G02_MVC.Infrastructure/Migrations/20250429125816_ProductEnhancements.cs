using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SG01G02_MVC.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProductEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "InStock",
                table: "Products",
                newName: "StockQuantity");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StockQuantity",
                table: "Products",
                newName: "InStock");
        }
    }
}
