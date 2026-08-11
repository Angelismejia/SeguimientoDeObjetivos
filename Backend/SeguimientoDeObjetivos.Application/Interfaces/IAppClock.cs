namespace Application.Interfaces
{
    /// <summary>
    /// El servidor corre en UTC, pero "hoy" y "ahora" para calculos de racha y
    /// recordatorios deben verse desde la zona horaria de la app (configurable),
    /// no desde UTC. Sin esto, cada Task/Objective/Badge que compara fechas contra
    /// DateTime.UtcNow.Date queda desalineado con las fechas locales que manda el
    /// frontend (ver TaskService.UpdateAsync).
    /// </summary>
    public interface IAppClock
    {
        DateTime Now { get; }
        DateTime Today { get; }

        /// <summary>
        /// Convierte una fecha/hora ya expresada en la zona de la app (p.ej. Today,
        /// o Today mas N dias) al UTC en que se guardan columnas como CreatedAt.
        /// Sirve para armar rangos "desde/hasta" comparables contra esas columnas.
        /// </summary>
        DateTime ToUtc(DateTime localDateTime);
    }
}
