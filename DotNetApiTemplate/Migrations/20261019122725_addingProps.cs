using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotNetApiTemplate.Migrations
{
    /// <inheritdoc />
    public partial class addingProps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BranchManagerId",
                table: "Branches",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Branches_BranchManagerId",
                table: "Branches",
                column: "BranchManagerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Branches_AspNetUsers_BranchManagerId",
                table: "Branches",
                column: "BranchManagerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Branches_AspNetUsers_BranchManagerId",
                table: "Branches");

            migrationBuilder.DropIndex(
                name: "IX_Branches_BranchManagerId",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "BranchManagerId",
                table: "Branches");
        }
    }
}
