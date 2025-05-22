using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SG01G02_MVC.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ForceFix_ReviewApiId_column : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
                migrationBuilder.Sql(@"
                    ALTER TABLE ""Products""
                    ALTER COLUMN ""ExternalReviewApiProductId""
                    TYPE TEXT USING ""ExternalReviewApiProductId""::TEXT;
                ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""Products""
                ALTER COLUMN ""ExternalReviewApiProductId""
                TYPE INTEGER USING ""ExternalReviewApiProductId""::INTEGER;
            ");
        }
    }
}