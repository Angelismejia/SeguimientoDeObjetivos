using Application.Common;
using Domain.Entities;
using Domain.Enums;

namespace SeguimientoDeObjetivos.Tests;

// El recordatorio de "es hora de tu tarea" buscaba ScheduledDate == hoy. Una
// recurrente tiene una sola ScheduledDate (el dia que empieza), asi que una
// tarea diaria avisaba el primer dia y nunca mas. Estos tests fijan la regla.
public class RecordatoriosDeRecurrentesTests
{
    private static readonly DateTime Lunes = new(2026, 11, 2);

    private static TaskItem Tarea(RecurrenceType tipo = RecurrenceType.None, string hora = "09:00", int? antes = null) => new()
    {
        Title = "Leer",
        ScheduledDate = Lunes,
        ScheduledTime = TimeSpan.Parse(hora),
        ReminderMinutesBefore = antes,
        IsRecurring = tipo != RecurrenceType.None,
        RecurrenceType = tipo
    };

    private static TimeSpan A(string hora) => TimeSpan.Parse(hora);

    [Fact]
    public void Una_diaria_avisa_tambien_los_dias_siguientes()
    {
        var diaria = Tarea(RecurrenceType.Daily);

        Assert.True(RecurrenciaDeTareas.DebeAvisar(diaria, hechaHoy: false, Lunes.AddDays(3), A("09:01")));
    }

    [Fact]
    public void Una_recurrente_ya_hecha_hoy_no_avisa()
    {
        var diaria = Tarea(RecurrenceType.Daily);

        Assert.False(RecurrenciaDeTareas.DebeAvisar(diaria, hechaHoy: true, Lunes.AddDays(3), A("09:01")));
    }

    [Fact]
    public void Una_semanal_solo_avisa_el_mismo_dia_de_la_semana()
    {
        var semanal = Tarea(RecurrenceType.Weekly);

        Assert.True(RecurrenciaDeTareas.OcurreEl(semanal, Lunes.AddDays(7)));
        Assert.False(RecurrenciaDeTareas.OcurreEl(semanal, Lunes.AddDays(8)));
    }

    [Fact]
    public void Cada_dos_semanas_se_salta_la_semana_del_medio()
    {
        var quincenal = Tarea(RecurrenceType.Weekly);
        quincenal.RepeatEveryWeeks = 2;

        Assert.False(RecurrenciaDeTareas.OcurreEl(quincenal, Lunes.AddDays(7)));
        Assert.True(RecurrenciaDeTareas.OcurreEl(quincenal, Lunes.AddDays(14)));
    }

    [Fact]
    public void No_avisa_despues_de_que_termina_la_repeticion()
    {
        var diaria = Tarea(RecurrenceType.Daily);
        diaria.EndRepeatDate = Lunes.AddDays(2);

        Assert.False(RecurrenciaDeTareas.OcurreEl(diaria, Lunes.AddDays(3)));
    }

    [Fact]
    public void Una_tarea_de_un_dia_no_avisa_otro_dia()
    {
        Assert.False(RecurrenciaDeTareas.OcurreEl(Tarea(), Lunes.AddDays(1)));
    }

    [Fact]
    public void Una_tarea_de_un_dia_completada_no_avisa()
    {
        var tarea = Tarea();
        tarea.Status = TaskItemStatus.Completed;

        Assert.False(RecurrenciaDeTareas.DebeAvisar(tarea, hechaHoy: false, Lunes, A("09:01")));
    }

    [Fact]
    public void Con_minutos_de_antelacion_avisa_antes_de_la_hora()
    {
        var tarea = Tarea(antes: 15);

        Assert.False(RecurrenciaDeTareas.DebeAvisar(tarea, false, Lunes, A("08:44")));
        Assert.True(RecurrenciaDeTareas.DebeAvisar(tarea, false, Lunes, A("08:45")));
    }

    // Si la API estuvo dormida y despierta horas despues, no manda un "es hora"
    // que ya no sirve.
    [Fact]
    public void Pasada_la_ventana_ya_no_avisa()
    {
        var tarea = Tarea();

        Assert.True(RecurrenciaDeTareas.DebeAvisar(tarea, false, Lunes, A("09:30")));
        Assert.False(RecurrenciaDeTareas.DebeAvisar(tarea, false, Lunes, A("09:31")));
    }

    [Fact]
    public void La_antelacion_no_cruza_la_medianoche()
    {
        var tarea = Tarea(hora: "00:10", antes: 30);

        Assert.Equal(TimeSpan.Zero, RecurrenciaDeTareas.MomentoDeAviso(tarea));
    }
}
