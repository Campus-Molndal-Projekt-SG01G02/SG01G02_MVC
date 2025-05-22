using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SG01G02_MVC.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Change_ReviewApiId_to_string : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ExternalReviewApiProductId",
                table: "Products",
                type: "TEXT",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "ExternalReviewApiProductId",
                table: "Products",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 10,
                oldNullable: true);
        }
    }
}
