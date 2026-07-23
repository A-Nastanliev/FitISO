using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitISO.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemovingNote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Note",
                table: "WorkoutExercises");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "WorkoutExercises",
                type: "TEXT",
                maxLength: 100,
                nullable: true);
        }
    }
}
