using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeguimientoDeObjetivos.Infrastructure.Migrations
{
    /// <summary>
    /// Pasa a "timestamp with time zone" las 21 columnas que guardan un INSTANTE.
    ///
    /// Antes todas las fechas eran "timestamp without time zone", asi que la base
    /// guardaba el numero y no de que huso era. Los valores siempre fueron UTC
    /// —todas las entidades los inicializan con DateTime.UtcNow— pero eso no estaba
    /// escrito en ninguna parte, asi que Npgsql los devolvia con Kind=Unspecified,
    /// el JSON salia sin la Z y el navegador los interpretaba como hora local:
    /// cada hora se mostraba corrida por el desfase de la zona.
    ///
    /// <para>
    /// Las fechas que representan un DIA no se tocan: EntryDate, StartDate,
    /// EndDate, LastCompletedDate, ScheduledDate, EndRepeatDate y
    /// TaskCompletions.Date. Convertirlas las correria de dia (un 2026-09-30 se
    /// volveria 29 de septiembre 20:00 en UTC-4), moviendo las tareas y las
    /// entradas del diario al dia anterior.
    /// </para>
    ///
    /// <para>
    /// La conversion va con USING ... AT TIME ZONE 'UTC' escrito a mano, y no con
    /// el AlterColumn que genera EF. Un ALTER COLUMN ... TYPE timestamptz a secas
    /// interpreta los valores que ya estan guardados segun el TimeZone de la sesion
    /// que corre la migracion: si esa sesion no esta en UTC, los corre en silencio.
    /// Diciendolo explicitamente, el resultado es el mismo sin importar desde donde
    /// se aplique.
    /// </para>
    /// </summary>
    public partial class FechasDeInstanteConZonaHoraria : Migration
    {
        private static readonly (string Tabla, string Columna)[] Instantes =
        {
            ("Users", "CreatedAt"),
            ("Users", "UpdatedAt"),
            ("UserBadges", "EarnedAt"),
            ("Tasks", "CreatedAt"),
            ("Tasks", "UpdatedAt"),
            ("Tasks", "CompletedAt"),
            ("TaskCompletions", "CompletedAt"),
            ("Streaks", "UpdatedAt"),
            ("Objectives", "CreatedAt"),
            ("Objectives", "UpdatedAt"),
            ("Notifications", "CreatedAt"),
            ("Notifications", "SentAt"),
            ("Messages", "SentAt"),
            ("Messages", "ReadAt"),
            ("FriendStreaks", "CreatedAt"),
            ("FriendStreakInvitations", "CreatedAt"),
            ("Follows", "CreatedAt"),
            ("DiaryEntries", "CreatedAt"),
            ("DiaryEntries", "UpdatedAt"),
            ("Categories", "CreatedAt"),
            ("Badges", "CreatedAt"),
        };

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var (tabla, columna) in Instantes)
            {
                migrationBuilder.Sql(
                    $"ALTER TABLE \"{tabla}\" " +
                    $"ALTER COLUMN \"{columna}\" TYPE timestamp with time zone " +
                    $"USING \"{columna}\" AT TIME ZONE 'UTC';");
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var (tabla, columna) in Instantes)
            {
                migrationBuilder.Sql(
                    $"ALTER TABLE \"{tabla}\" " +
                    $"ALTER COLUMN \"{columna}\" TYPE timestamp without time zone " +
                    $"USING \"{columna}\" AT TIME ZONE 'UTC';");
            }
        }
    }
}
