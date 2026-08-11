using Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Common
{
    /// <summary>
    /// Traduce UtcNow a la zona horaria configurada en "AppTimeZone" (appsettings.json).
    /// Acepta tanto el ID de Windows ("Central America Standard Time") como el IANA
    /// ("America/Guatemala") porque .NET 8 resuelve ambos sin importar el SO donde
    /// corra el servidor. Si el ID no existe o no se configuro nada, cae a UTC en vez
    /// de tirar una excepcion al arrancar la app.
    /// </summary>
    public class AppClock : IAppClock
    {
        private readonly TimeZoneInfo _timeZone;

        public AppClock(IConfiguration configuration)
        {
            var configuredId = configuration["AppTimeZone"];
            _timeZone = TryFindTimeZone(configuredId) ?? TimeZoneInfo.Utc;
        }

        public DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone);
        public DateTime Today => Now.Date;

        public DateTime ToUtc(DateTime localDateTime) =>
            TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified), _timeZone);

        private static TimeZoneInfo? TryFindTimeZone(string? id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
                return null;
            }
            catch (InvalidTimeZoneException)
            {
                return null;
            }
        }
    }
}
