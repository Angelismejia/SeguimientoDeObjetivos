using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeguimientoDeObjetivos.Infraestructure.Migrations
{
    /// <inheritdoc />
    public partial class DropLegacyKeycloakColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_KeycloakUserId' AND object_id = OBJECT_ID('Users'))
                    DROP INDEX IX_Users_KeycloakUserId ON Users;
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE Name = 'KeycloakUserId' AND Object_ID = Object_ID('Users'))
                    ALTER TABLE Users DROP COLUMN KeycloakUserId;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "KeycloakUserId",
                table: "Users",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Users_KeycloakUserId",
                table: "Users",
                column: "KeycloakUserId",
                unique: true);
        }
    }
}
