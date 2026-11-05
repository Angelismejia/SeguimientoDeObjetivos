using Domain.Entities;
using Domain.Enums;

namespace Application.Common
{
    /// <summary>
    /// Que dias le tocan a una tarea, y cuando avisarle al usuario que es la hora.
    ///
    /// La regla de recurrencia es copia exacta de <c>taskOccursOn</c> del front
    /// (Frontend/src/app/core/utils/task-recurrence.util.ts): si se cambia una,
    /// hay que cambiar la otra. Antes solo existia en el front, y por eso el
    /// recordatorio del backend buscaba tareas con ScheduledDate == hoy: una tarea
    /// diaria avisaba el primer dia y nunca mas.
    /// </summary>
    public static class RecurrenciaDeTareas
    {
        /// <summary>
        /// Cuanto tarde puede llegar un recordatorio. Si la API estuvo dormida
        /// (Azure apaga la app sin trafico) y despierta a las 18:00, no tiene
        /// sentido avisar "es hora" de una tarea de las 9:00.
        /// </summary>
        public static readonly TimeSpan VentanaDeAviso = TimeSpan.FromMinutes(30);

        public static bool OcurreEl(TaskItem task, DateTime dia)
        {
            var inicio = task.ScheduledDate.Date;
            dia = dia.Date;
            if (dia < inicio) return false;
            if (task.EndRepeatDate.HasValue && dia > task.EndRepeatDate.Value.Date) return false;

            if (!task.IsRecurring || task.RecurrenceType == RecurrenceType.None)
                return dia == inicio;

            var dias = (int)(dia - inicio).TotalDays;
            return task.RecurrenceType switch
            {
                RecurrenceType.Daily => true,
                RecurrenceType.Weekly => dias % (7 * (task.RepeatEveryWeeks is > 0 ? task.RepeatEveryWeeks.Value : 1)) == 0,
                RecurrenceType.Monthly => dia.Day == inicio.Day,
                RecurrenceType.Yearly => dia.Day == inicio.Day && dia.Month == inicio.Month,
                _ => false
            };
        }

        /// <summary>
        /// Si toca avisar ahora de esta tarea. <paramref name="hechaHoy"/> es si la
        /// recurrente ya tiene su TaskCompletion de hoy (las de un dia usan Status).
        /// </summary>
        public static bool DebeAvisar(TaskItem task, bool hechaHoy, DateTime hoy, TimeSpan ahora)
        {
            if (task.ScheduledTime is null) return false;
            if (task.Status == TaskItemStatus.Skipped) return false;
            if (!OcurreEl(task, hoy)) return false;

            var hecha = task.IsRecurring ? hechaHoy : task.Status == TaskItemStatus.Completed;
            if (hecha) return false;

            var momento = MomentoDeAviso(task);
            return momento <= ahora && ahora - momento <= VentanaDeAviso;
        }

        /// <summary>La hora de la tarea menos los minutos de antelacion, sin bajar de 00:00.</summary>
        public static TimeSpan MomentoDeAviso(TaskItem task)
        {
            var momento = task.ScheduledTime!.Value - TimeSpan.FromMinutes(task.ReminderMinutesBefore ?? 0);
            return momento < TimeSpan.Zero ? TimeSpan.Zero : momento;
        }
    }
}
