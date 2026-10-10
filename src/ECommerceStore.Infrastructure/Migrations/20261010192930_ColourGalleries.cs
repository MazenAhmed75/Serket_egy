using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerceStore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ColourGalleries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProductColorId",
                table: "ProductImages",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ProductColorId",
                table: "ProductImages",
                column: "ProductColorId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductImages_ProductColors_ProductColorId",
                table: "ProductImages",
                column: "ProductColorId",
                principalTable: "ProductColors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductImages_ProductColors_ProductColorId",
                table: "ProductImages");

            migrationBuilder.DropIndex(
                name: "IX_ProductImages_ProductColorId",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "ProductColorId",
                table: "ProductImages");
        }
    }
}
