using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeguimientoDeObjetivos.Infraestructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskEmojiAndColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Tasks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Emoji",
                table: "Tasks",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Color",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "Emoji",
                table: "Tasks");
        }
    }
}
