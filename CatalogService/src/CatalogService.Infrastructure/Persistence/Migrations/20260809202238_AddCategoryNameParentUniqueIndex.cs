using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CatalogService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryNameParentUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true,
                filter: "\"ParentCategoryId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name_ParentCategoryId",
                table: "Categories",
                columns: new[] { "Name", "ParentCategoryId" },
                unique: true,
                filter: "\"ParentCategoryId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Categories_Name",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_Name_ParentCategoryId",
                table: "Categories");
        }
    }
}
