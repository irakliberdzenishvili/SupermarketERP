using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupermarketERP.Migrations
{
    /// <inheritdoc />
    public partial class propos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "StockTransfers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelledByUserId",
                table: "StockTransfers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "StockTransfers");

            migrationBuilder.DropColumn(
                name: "CancelledByUserId",
                table: "StockTransfers");
        }
    }
}
