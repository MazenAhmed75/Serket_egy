using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerceStore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OrderStreetAndBuilding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BuildingNumber",
                table: "Orders",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Street",
                table: "Orders",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuildingNumber",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Street",
                table: "Orders");
        }
    }
}
