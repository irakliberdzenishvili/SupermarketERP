using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotNetApiTemplate.Migrations
{
    /// <inheritdoc />
    public partial class addingEmployees3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Branches_BranchId1",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_BranchId1",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "BranchId1",
                table: "Employees");

            migrationBuilder.AlterColumn<Guid>(
                name: "BranchManagerId",
                table: "Branches",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Branches_Employees_BranchManagerId",
                table: "Branches",
                column: "BranchManagerId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Branches_Employees_BranchManagerId",
                table: "Branches");

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId1",
                table: "Employees",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BranchManagerId",
                table: "Branches",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_BranchId1",
                table: "Employees",
                column: "BranchId1",
                unique: true,
                filter: "[BranchId1] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Branches_BranchId1",
                table: "Employees",
                column: "BranchId1",
                principalTable: "Branches",
                principalColumn: "Id");
        }
    }
}
