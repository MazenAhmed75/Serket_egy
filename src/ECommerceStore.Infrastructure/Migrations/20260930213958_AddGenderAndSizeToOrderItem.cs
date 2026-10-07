using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerceStore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGenderAndSizeToOrderItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "OrderItems",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Size",
                table: "OrderItems",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Gender",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "Size",
                table: "OrderItems");
        }
    }
}
