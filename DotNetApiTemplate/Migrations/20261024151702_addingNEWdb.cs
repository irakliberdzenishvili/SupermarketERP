using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotNetApiTemplate.Migrations
{
    /// <inheritdoc />
    public partial class addingNEWdb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Branches_Employees_BranchManagerId",
                table: "Branches");

            migrationBuilder.DropForeignKey(
                name: "FK_Branches_Warehouses_WarehouseId",
                table: "Branches");

            migrationBuilder.DropForeignKey(
                name: "FK_WarehouseManagers_Employees_EmployeeId",
                table: "WarehouseManagers");

            migrationBuilder.DropForeignKey(
                name: "FK_WarehouseManagers_Warehouses_WarehouseId",
                table: "WarehouseManagers");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseManagers_EmployeeId",
                table: "WarehouseManagers");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "WarehouseManagers");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "WarehouseManagers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "WarehouseManagers",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "BranchWorkers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HireDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId1 = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchWorkers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BranchWorkers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BranchWorkers_AspNetUsers_UserId1",
                        column: x => x.UserId1,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BranchWorkers_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WarehouseWorkers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HireDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseWorkers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseWorkers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WarehouseWorkers_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseManagers_UserId",
                table: "WarehouseManagers",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BranchWorkers_BranchId",
                table: "BranchWorkers",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_BranchWorkers_UserId",
                table: "BranchWorkers",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BranchWorkers_UserId1",
                table: "BranchWorkers",
                column: "UserId1",
                unique: true,
                filter: "[UserId1] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseWorkers_UserId",
                table: "WarehouseWorkers",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseWorkers_WarehouseId",
                table: "WarehouseWorkers",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Branches_BranchWorkers_BranchManagerId",
                table: "Branches",
                column: "BranchManagerId",
                principalTable: "BranchWorkers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Branches_Warehouses_WarehouseId",
                table: "Branches",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WarehouseManagers_AspNetUsers_UserId",
                table: "WarehouseManagers",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WarehouseManagers_Warehouses_WarehouseId",
                table: "WarehouseManagers",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Branches_BranchWorkers_BranchManagerId",
                table: "Branches");

            migrationBuilder.DropForeignKey(
                name: "FK_Branches_Warehouses_WarehouseId",
                table: "Branches");

            migrationBuilder.DropForeignKey(
                name: "FK_WarehouseManagers_AspNetUsers_UserId",
                table: "WarehouseManagers");

            migrationBuilder.DropForeignKey(
                name: "FK_WarehouseManagers_Warehouses_WarehouseId",
                table: "WarehouseManagers");

            migrationBuilder.DropTable(
                name: "BranchWorkers");

            migrationBuilder.DropTable(
                name: "WarehouseWorkers");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseManagers_UserId",
                table: "WarehouseManagers");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "WarehouseManagers");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "WarehouseManagers");

            migrationBuilder.AddColumn<Guid>(
                name: "EmployeeId",
                table: "WarehouseManagers",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    HireDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
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
                name: "IX_WarehouseManagers_EmployeeId",
                table: "WarehouseManagers",
                column: "EmployeeId",
                unique: true);

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
                name: "FK_Branches_Employees_BranchManagerId",
                table: "Branches",
                column: "BranchManagerId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Branches_Warehouses_WarehouseId",
                table: "Branches",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WarehouseManagers_Employees_EmployeeId",
                table: "WarehouseManagers",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WarehouseManagers_Warehouses_WarehouseId",
                table: "WarehouseManagers",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
