using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotNetApiTemplate.Migrations
{
    /// <inheritdoc />
    public partial class addingEmployees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Branches_AspNetUsers_BranchManagerId",
                table: "Branches");

            migrationBuilder.DropIndex(
                name: "IX_Branches_BranchManagerId",
                table: "Branches");

            migrationBuilder.AlterColumn<Guid>(
                name: "BranchManagerId",
                table: "Branches",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchManagerId1",
                table: "Branches",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HireDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Employees_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Employees_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Branches_BranchManagerId",
                table: "Branches",
                column: "BranchManagerId",
                unique: true,
                filter: "[BranchManagerId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Branches_BranchManagerId1",
                table: "Branches",
                column: "BranchManagerId1");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_BranchId",
                table: "Employees",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_UserId",
                table: "Employees",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Branches_Employees_BranchManagerId1",
                table: "Branches",
                column: "BranchManagerId1",
                principalTable: "Employees",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Branches_Employees_BranchManagerId1",
                table: "Branches");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Branches_BranchManagerId",
                table: "Branches");

            migrationBuilder.DropIndex(
                name: "IX_Branches_BranchManagerId1",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "BranchManagerId1",
                table: "Branches");

            migrationBuilder.AlterColumn<string>(
                name: "BranchManagerId",
                table: "Branches",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Branches_BranchManagerId",
                table: "Branches",
                column: "BranchManagerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Branches_AspNetUsers_BranchManagerId",
                table: "Branches",
                column: "BranchManagerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
