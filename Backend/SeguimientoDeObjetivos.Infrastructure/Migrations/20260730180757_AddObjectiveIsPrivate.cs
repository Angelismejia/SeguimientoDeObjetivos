using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeguimientoDeObjetivos.Infraestructure.Migrations
{
    /// <inheritdoc />
    public partial class AddObjectiveIsPrivate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPrivate",
                table: "Objectives",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPrivate",
                table: "Objectives");
        }
    }
}
