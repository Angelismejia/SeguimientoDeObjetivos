using Application.DTOs.Tasks;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;

namespace SeguimientoDeObjetivos.Tests;

// "Recurrente" y "sin frecuencia" no pueden ser verdad a la vez. Se guardaba sin
// protestar porque el servicio hacia `dto.RecurrenceType ?? RecurrenceType.None`:
// el ?? tapaba el hueco en vez de senalarlo, y la tarea quedaba marcada como
// recurrente pero sin repetirse nunca.
public class ValidacionDeRecurrenciaTests
{
    private static readonly DateTime Dia = new(2026, 8, 17);

    private static TaskService ServicioDeTareas(TaskItem? existente = null) =>
        new(new TaskRepoSimple(existente), new CompletionRepoVacio(),
            new UnitOfWorkFalso(), new BadgeAwardFalso());

    private static CreateTaskDto CrearTarea(bool recurrente, RecurrenceType? tipo) =>
        new()
        {
            Title = "Estudiar",
            ScheduledDate = Dia,
            IsRecurring = recurrente,
            RecurrenceType = tipo
        };

    private static UpdateTaskDto EditarTarea(bool recurrente, RecurrenceType? tipo) =>
        new()
        {
            Title = "Estudiar",
            ScheduledDate = Dia,
            IsRecurring = recurrente,
            RecurrenceType = tipo,
            Priority = TaskPriority.Medium,
            Status = TaskItemStatus.Pending
        };

    [Fact]
    public async Task Una_tarea_recurrente_necesita_una_frecuencia()
    {
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => ServicioDeTareas().CreateAsync(1, CrearTarea(true, RecurrenceType.None)));

        Assert.Contains("frecuencia", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // Mismo caso, pero con el campo directamente ausente en el JSON.
    [Fact]
    public async Task Tampoco_vale_omitir_la_frecuencia()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(
            () => ServicioDeTareas().CreateAsync(1, CrearTarea(true, null)));
    }

    [Fact]
    public async Task Una_tarea_recurrente_con_frecuencia_se_guarda()
    {
        var dto = await ServicioDeTareas().CreateAsync(1, CrearTarea(true, RecurrenceType.Weekly));

        Assert.True(dto.IsRecurring);
        Assert.Equal(RecurrenceType.Weekly, dto.RecurrenceType);
    }

    // Al reves la contradiccion no bloquea, se limpia: una tarea que no es
    // recurrente no puede quedarse con el tipo que tenia de antes colgado.
    [Fact]
    public async Task Una_tarea_no_recurrente_pierde_la_frecuencia_que_le_manden()
    {
        var dto = await ServicioDeTareas().CreateAsync(1, CrearTarea(false, RecurrenceType.Weekly));

        Assert.False(dto.IsRecurring);
        Assert.Equal(RecurrenceType.None, dto.RecurrenceType);
    }

    [Fact]
    public async Task Desmarcar_recurrente_al_editar_tambien_limpia_la_frecuencia()
    {
        var existente = new TaskItem
        {
            Id = 3,
            UserId = 1,
            Title = "Estudiar",
            ScheduledDate = Dia,
            IsRecurring = true,
            RecurrenceType = RecurrenceType.Daily
        };

        var actualizada = await ServicioDeTareas(existente)
            .UpdateAsync(3, EditarTarea(false, RecurrenceType.Daily));

        Assert.Equal(RecurrenceType.None, actualizada.RecurrenceType);
    }

    // ── Filas que ya estaban mal antes de que existiera la regla ──
    // Misma leccion que con los rangos de fechas: el desplegable ofrecia "None",
    // asi que hay tareas guardadas en ese estado. Marcarlas como hechas manda un
    // PUT completo que reenvia la recurrencia sin tocarla; si validaramos siempre,
    // esas tareas quedarian imposibles de completar y de editar para arreglarlas.

    private static TaskItem TareaRecurrenteSinFrecuencia() => new()
    {
        Id = 3,
        UserId = 1,
        Title = "Estudiar",
        ScheduledDate = Dia,
        IsRecurring = true,
        RecurrenceType = RecurrenceType.None
    };

    [Fact]
    public async Task Una_tarea_ya_guardada_sin_frecuencia_se_puede_seguir_actualizando()
    {
        var actualizada = await ServicioDeTareas(TareaRecurrenteSinFrecuencia())
            .UpdateAsync(3, new UpdateTaskDto
            {
                Title = "Estudiar",
                ScheduledDate = Dia,
                IsRecurring = true,
                RecurrenceType = RecurrenceType.None,
                Priority = TaskPriority.Medium,
                Status = TaskItemStatus.Completed
            });

        Assert.Equal(TaskItemStatus.Completed, actualizada.Status);
    }

    [Fact]
    public async Task Pero_al_elegir_frecuencia_queda_arreglada()
    {
        var actualizada = await ServicioDeTareas(TareaRecurrenteSinFrecuencia())
            .UpdateAsync(3, EditarTarea(true, RecurrenceType.Daily));

        Assert.Equal(RecurrenceType.Daily, actualizada.RecurrenceType);
    }
}
