using Application.DTOs.Notifications;
using Application.DTOs.Objectives;
using Application.DTOs.Tasks;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Services;
using Domain.Entities;
using Domain.Enums;

namespace SeguimientoDeObjetivos.Tests;

// Un objetivo que termina antes de empezar, o una tarea que termina antes de la
// hora en que arranca, no son datos posibles. Las DataAnnotations de los DTOs no
// pueden expresarlo porque miran un campo a la vez, asi que hasta ahora no lo
// comprobaba nadie y la fila se guardaba igual.
public class ValidacionDeRangosTests
{
    private static readonly DateTime Dia = new(2026, 8, 17);

    // ── Objetivos ─────────────────────────────────────────

    private static ObjectiveService ServicioDeObjetivos(Objective? existente = null) =>
        new(new ObjectiveRepoFalso(existente), new UnitOfWorkFalso(),
            new BadgeAwardFalso(), new NotificationServiceFalso());

    private static CreateObjectiveDto CrearObjetivo(DateTime? inicio, DateTime? fin) =>
        new() { Title = "Aprender Angular", StartDate = inicio, EndDate = fin };

    private static UpdateObjectiveDto EditarObjetivo(DateTime? inicio, DateTime? fin) =>
        new() { Title = "Aprender Angular", StartDate = inicio, EndDate = fin, Status = ObjectiveStatus.InProgress };

    [Fact]
    public async Task Un_objetivo_no_puede_terminar_antes_de_empezar()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => ServicioDeObjetivos().CreateAsync(1, CrearObjetivo(Dia, Dia.AddDays(-1))));

        Assert.Contains("anterior", ex.Message);
    }

    [Fact]
    public async Task Editar_un_objetivo_tampoco_permite_invertir_el_rango()
    {
        var existente = new Objective { Id = 7, UserId = 1, Title = "Aprender Angular" };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => ServicioDeObjetivos(existente).UpdateAsync(7, EditarObjetivo(Dia, Dia.AddDays(-1))));
    }

    [Fact]
    public async Task Un_objetivo_con_el_rango_en_orden_se_guarda()
    {
        var dto = await ServicioDeObjetivos().CreateAsync(1, CrearObjetivo(Dia, Dia.AddDays(30)));
        Assert.Equal("Aprender Angular", dto.Title);
    }

    // Ambas fechas son opcionales: media fecha no es una fecha invalida.
    [Fact]
    public async Task Un_objetivo_con_media_fecha_se_guarda()
    {
        Assert.NotNull(await ServicioDeObjetivos().CreateAsync(1, CrearObjetivo(Dia, null)));
        Assert.NotNull(await ServicioDeObjetivos().CreateAsync(1, CrearObjetivo(null, Dia)));
        Assert.NotNull(await ServicioDeObjetivos().CreateAsync(1, CrearObjetivo(null, null)));
    }

    // El usuario elige dias, no horas: empezar y terminar el mismo dia vale.
    [Fact]
    public async Task Empezar_y_terminar_el_mismo_dia_vale()
    {
        Assert.NotNull(await ServicioDeObjetivos()
            .CreateAsync(1, CrearObjetivo(Dia, Dia.AddHours(9))));
    }

    // ── Tareas ────────────────────────────────────────────

    private static TaskService ServicioDeTareas(TaskItem? existente = null) =>
        new(new TaskRepoSimple(existente), new CompletionRepoVacio(),
            new UnitOfWorkFalso(), new BadgeAwardFalso());

    private static CreateTaskDto CrearTarea(TimeSpan? inicio, TimeSpan? fin, DateTime? finRepeticion = null) =>
        new()
        {
            Title = "Estudiar",
            ScheduledDate = Dia,
            ScheduledTime = inicio,
            EndTime = fin,
            EndRepeatDate = finRepeticion
        };

    [Fact]
    public async Task Una_tarea_no_puede_terminar_antes_de_su_hora_de_inicio()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => ServicioDeTareas().CreateAsync(1, CrearTarea(new TimeSpan(18, 0, 0), new TimeSpan(9, 0, 0))));

        Assert.Contains("hora", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task La_repeticion_no_puede_caducar_antes_de_la_primera_vez()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => ServicioDeTareas().CreateAsync(1, CrearTarea(null, null, Dia.AddDays(-1))));

        Assert.Contains("repetición", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Editar_una_tarea_tambien_valida_el_horario()
    {
        var existente = new TaskItem { Id = 3, UserId = 1, Title = "Estudiar", ScheduledDate = Dia };
        var dto = new UpdateTaskDto
        {
            Title = "Estudiar",
            ScheduledDate = Dia,
            ScheduledTime = new TimeSpan(18, 0, 0),
            EndTime = new TimeSpan(9, 0, 0),
            Priority = TaskPriority.Medium,
            Status = TaskItemStatus.Pending
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => ServicioDeTareas(existente).UpdateAsync(3, dto));
    }

    // La hora es opcional (una tarea puede no tener horario) y una recurrente sin
    // EndRepeatDate es una que se repite indefinidamente.
    [Fact]
    public async Task Una_tarea_sin_horas_o_sin_fin_de_repeticion_se_guarda()
    {
        Assert.NotNull(await ServicioDeTareas().CreateAsync(1, CrearTarea(null, null)));
        Assert.NotNull(await ServicioDeTareas().CreateAsync(1, CrearTarea(new TimeSpan(9, 0, 0), null)));
        Assert.NotNull(await ServicioDeTareas().CreateAsync(1, CrearTarea(null, new TimeSpan(18, 0, 0))));
    }

    [Fact]
    public async Task Una_tarea_con_el_horario_en_orden_se_guarda()
    {
        var dto = await ServicioDeTareas()
            .CreateAsync(1, CrearTarea(new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0), Dia.AddMonths(1)));

        Assert.Equal("Estudiar", dto.Title);
    }

    [Fact]
    public async Task Empezar_y_terminar_a_la_misma_hora_vale()
    {
        Assert.NotNull(await ServicioDeTareas()
            .CreateAsync(1, CrearTarea(new TimeSpan(9, 0, 0), new TimeSpan(9, 0, 0))));
    }
}

// ── Dobles de prueba propios de este archivo ──────────────
// Los de Fakes.cs lanzan NotSupportedException en CreateAsync/UpdateAsync porque
// las rachas nunca escriben; aca si hace falta que escribir funcione.

internal sealed class ObjectiveRepoFalso : IObjectiveRepository
{
    private readonly Objective? _existente;
    public ObjectiveRepoFalso(Objective? existente) => _existente = existente;

    public Task<Objective?> GetByIdAsync(int id) => Task.FromResult(_existente);
    public Task<Objective> CreateAsync(Objective o) { o.Id = 1; return Task.FromResult(o); }
    public Task<Objective> UpdateAsync(Objective o) => Task.FromResult(o);
    public Task<IEnumerable<Objective>> GetByUserIdAsync(int userId) => throw new NotSupportedException();
    public Task<bool> DeleteAsync(int id) => throw new NotSupportedException();
}

internal sealed class TaskRepoSimple : ITaskRepository
{
    private readonly TaskItem? _existente;
    public TaskRepoSimple(TaskItem? existente) => _existente = existente;

    public Task<TaskItem?> GetByIdAsync(int id) => Task.FromResult(_existente);
    public Task<TaskItem> CreateAsync(TaskItem t) { t.Id = 1; return Task.FromResult(t); }
    public Task<TaskItem> UpdateAsync(TaskItem t) => Task.FromResult(t);
    public Task<IEnumerable<TaskItem>> GetByUserIdAsync(int userId) => throw new NotSupportedException();
    public Task<IEnumerable<TaskItem>> GetByObjectiveIdAsync(int objectiveId) => throw new NotSupportedException();
    public Task<bool> DeleteAsync(int id) => throw new NotSupportedException();
}

internal sealed class CompletionRepoVacio : ITaskCompletionRepository
{
    public Task<IEnumerable<TaskCompletion>> GetByUserIdAsync(int userId) =>
        Task.FromResult<IEnumerable<TaskCompletion>>(new List<TaskCompletion>());
    public Task<IEnumerable<TaskCompletion>> GetByTaskIdAsync(int taskId) =>
        Task.FromResult<IEnumerable<TaskCompletion>>(new List<TaskCompletion>());
    public Task<TaskCompletion?> GetAsync(int taskId, DateTime date) => Task.FromResult<TaskCompletion?>(null);
    public Task<TaskCompletion> CreateAsync(TaskCompletion c) => Task.FromResult(c);
    public Task<bool> DeleteAsync(int taskId, DateTime date) => Task.FromResult(true);
}

internal sealed class BadgeAwardFalso : IBadgeAwardService
{
    public Task CheckAndAwardAsync(int userId) => Task.CompletedTask;
}

internal sealed class NotificationServiceFalso : INotificationService
{
    public Task<NotificationDto> CreateAsync(CreateNotificationDto dto) => Task.FromResult(new NotificationDto());
    public Task<IEnumerable<NotificationDto>> GetByUserIdAsync(int userId) => throw new NotSupportedException();
    public Task<IEnumerable<NotificationDto>> GetUnreadByUserIdAsync(int userId) => throw new NotSupportedException();
    public Task<NotificationDto> GetByIdAsync(int id) => throw new NotSupportedException();
    public Task MarkAsReadAsync(int id) => throw new NotSupportedException();
    public Task<int> MarkAllAsReadAsync(int userId) => throw new NotSupportedException();
    public Task DeleteAsync(int id) => throw new NotSupportedException();
}
