using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication1.Data.Migrations
{
    /// <inheritdoc />
    public partial class UserTypeUpdated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsShopkeeper",
                table: "AspNetUsers",
                newName: "isShopkeeper");

            migrationBuilder.RenameColumn(
                name: "HasRegisteredBrand",
                table: "AspNetUsers",
                newName: "hasRegisteredBrand");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "isShopkeeper",
                table: "AspNetUsers",
                newName: "IsShopkeeper");

            migrationBuilder.RenameColumn(
                name: "hasRegisteredBrand",
                table: "AspNetUsers",
                newName: "HasRegisteredBrand");
        }
    }
}
