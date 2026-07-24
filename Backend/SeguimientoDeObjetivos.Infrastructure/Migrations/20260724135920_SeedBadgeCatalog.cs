using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeguimientoDeObjetivos.Infraestructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedBadgeCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var seededAt = new DateTime(2026, 7, 24, 0, 0, 0, DateTimeKind.Utc);

            migrationBuilder.InsertData(
                table: "Badges",
                columns: new[] { "Id", "Name", "Description", "BadgeType", "Icon", "CreatedAt" },
                values: new object[,]
                {
                    { 1, "Primer paso", "Completaste tu primera tarea.", "first_task", "check_circle", seededAt },
                    { 2, "En marcha", "Completaste 10 tareas.", "tasks_10", "task_alt", seededAt },
                    { 3, "Imparable", "Completaste 50 tareas.", "tasks_50", "bolt", seededAt },
                    { 4, "Meta cumplida", "Completaste tu primer objetivo.", "first_objective", "flag", seededAt },
                    { 5, "Coleccionista de metas", "Completaste 5 objetivos.", "objectives_5", "military_tech", seededAt },
                    { 6, "Constancia", "3 días seguidos completando tareas.", "streak_3", "local_fire_department", seededAt },
                    { 7, "Una semana fuerte", "7 días seguidos completando tareas.", "streak_7", "whatshot", seededAt },
                    { 8, "Hábito de acero", "30 días seguidos completando tareas.", "streak_30", "emoji_events", seededAt }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "Badges", keyColumn: "Id", keyValues: new object[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        }
    }
}
