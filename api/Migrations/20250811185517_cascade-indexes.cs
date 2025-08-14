using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class cascadeindexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notes_Tasks_TaskId",
                table: "Notes");

            migrationBuilder.AddForeignKey(
                name: "FK_Notes_Tasks_TaskId",
                table: "Notes",
                column: "TaskId",
                principalTable: "Tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notes_Tasks_TaskId",
                table: "Notes");

            migrationBuilder.AddForeignKey(
                name: "FK_Notes_Tasks_TaskId",
                table: "Notes",
                column: "TaskId",
                principalTable: "Tasks",
                principalColumn: "Id");
        }
    }
}
