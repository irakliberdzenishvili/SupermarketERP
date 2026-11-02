using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupermarketERP.Migrations
{
    /// <inheritdoc />
    public partial class addingProps24 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "StockTransfers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectedByUserId",
                table: "StockTransfers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "StockTransfers");

            migrationBuilder.DropColumn(
                name: "RejectedByUserId",
                table: "StockTransfers");
        }
    }
}
