using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerceStore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductColors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ColorName",
                table: "OrderItems",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductColorId",
                table: "OrderItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductColors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    HexCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StockQuantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductColors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductColors_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ProductColorId",
                table: "OrderItems",
                column: "ProductColorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductColors_ProductId",
                table: "ProductColors",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_ProductColors_ProductColorId",
                table: "OrderItems",
                column: "ProductColorId",
                principalTable: "ProductColors",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_ProductColors_ProductColorId",
                table: "OrderItems");

            migrationBuilder.DropTable(
                name: "ProductColors");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_ProductColorId",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "ColorName",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "ProductColorId",
                table: "OrderItems");
        }
    }
}
