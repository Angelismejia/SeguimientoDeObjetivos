using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.Common
{
    /// <summary>
    /// Serializa un instante marcandolo como UTC, para que el JSON lleve la Z final.
    ///
    /// Hace falta porque las columnas son "timestamp without time zone": Npgsql
    /// devuelve los valores con DateTimeKind.Unspecified y System.Text.Json, al no
    /// saber de que zona son, los escribe sin sufijo ("2026-09-30T20:40:05"). El
    /// navegador interpreta una fecha sin zona como hora LOCAL, asi que el valor
    /// —que es UTC— se mostraba corrido por el desfase de la zona.
    ///
    /// Marcarlo como UTC es correcto porque TODAS las marcas de tiempo de las
    /// entidades nacen de DateTime.UtcNow. AppClock nunca se persiste: solo se usa
    /// para comparar (rachas, recordatorios), nunca para guardar.
    ///
    /// <para>
    /// Va propiedad por propiedad y NO como conversor global, a proposito. Hay
    /// columnas DateTime que representan un DIA y no un instante: ScheduledDate,
    /// EntryDate, StartDate, EndDate, EndRepeatDate y las fechas de CompletedDates.
    /// Marcarlas como UTC las correria de dia: un "2026-09-30T00:00:00" se mostraria
    /// como el 29 de septiembre a las 20:00 en una zona UTC-4. Esas se quedan sin
    /// zona, que es lo correcto para un dia.
    /// </para>
    /// </summary>
    public class InstanteUtcJsonConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            DateTime.SpecifyKind(reader.GetDateTime(), DateTimeKind.Utc);

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
            writer.WriteStringValue(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }

    /// <inheritdoc cref="InstanteUtcJsonConverter"/>
    public class InstanteUtcNullableJsonConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TokenType == JsonTokenType.Null
                ? null
                : DateTime.SpecifyKind(reader.GetDateTime(), DateTimeKind.Utc);

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteStringValue(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc));
            else
                writer.WriteNullValue();
        }
    }
}
