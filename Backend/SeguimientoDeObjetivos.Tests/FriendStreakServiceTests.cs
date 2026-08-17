using Application.Services;
using Domain.Entities;
using Domain.Enums;

namespace SeguimientoDeObjetivos.Tests;

// La racha compartida cuenta los dias seguidos en que AMBOS completaron algo.
// Antes leia una sola fecha por tarea, asi que una tarea recurrente aportaba un
// unico dia y la racha se cortaba sola aunque los dos hubieran cumplido.
public class FriendStreakServiceTests
{
    private const int Ana = 1;
    private const int Beto = 2;
    private static readonly DateTime Hoy = new(2026, 8, 17);

    private static TaskItem Recurrente(int id, int userId) => new()
    {
        Id = id,
        UserId = userId,
        Title = "Correr",
        IsRecurring = true,
        RecurrenceType = RecurrenceType.Daily,
        ScheduledDate = Hoy.AddDays(-10),
        Status = TaskItemStatus.Pending
    };

    private static TaskItem DeUnDia(int id, int userId, DateTime fecha) => new()
    {
        Id = id,
        UserId = userId,
        Title = "Entregar informe",
        IsRecurring = false,
        ScheduledDate = fecha,
        Status = TaskItemStatus.Completed
    };

    private static TaskCompletion Completada(int taskId, DateTime dia) => new()
    {
        TaskId = taskId,
        Date = dia.Date,
        CompletedAt = dia
    };

    private static async Task<int> RachaEntreAsync(
        Dictionary<int, List<TaskItem>> tareas,
        Dictionary<int, List<TaskCompletion>> completados)
    {
        var servicio = new FriendStreakService(
            new InvitationRepoFalso(),
            new FriendStreakRepoFalso(new FriendStreak { Id = 1, UserAId = Ana, UserBId = Beto }),
            new FollowRepoFalso(),
            new UserRepoFalso(),
            new TaskRepoFalso(tareas),
            new CompletionRepoFalso(completados),
            new UnitOfWorkFalso(),
            new RelojFijo(Hoy));

        var rachas = await servicio.GetForUserAsync(Ana);
        return rachas.Single().CurrentStreak;
    }

    [Fact]
    public async Task Sin_actividad_la_racha_es_cero()
    {
        var racha = await RachaEntreAsync(
            new() { [Ana] = new() { Recurrente(10, Ana) }, [Beto] = new() { Recurrente(20, Beto) } },
            new() { [Ana] = new(), [Beto] = new() });

        Assert.Equal(0, racha);
    }

    // El caso que estaba roto: tres dias seguidos con una tarea recurrente.
    [Fact]
    public async Task Tres_dias_seguidos_de_ambos_dan_racha_de_tres()
    {
        var dias = new[] { Hoy, Hoy.AddDays(-1), Hoy.AddDays(-2) };

        var racha = await RachaEntreAsync(
            new() { [Ana] = new() { Recurrente(10, Ana) }, [Beto] = new() { Recurrente(20, Beto) } },
            new()
            {
                [Ana] = dias.Select(d => Completada(10, d)).ToList(),
                [Beto] = dias.Select(d => Completada(20, d)).ToList()
            });

        Assert.Equal(3, racha);
    }

    [Fact]
    public async Task Si_uno_solo_cumple_la_racha_no_avanza()
    {
        var dias = new[] { Hoy, Hoy.AddDays(-1), Hoy.AddDays(-2) };

        var racha = await RachaEntreAsync(
            new() { [Ana] = new() { Recurrente(10, Ana) }, [Beto] = new() { Recurrente(20, Beto) } },
            new()
            {
                [Ana] = dias.Select(d => Completada(10, d)).ToList(),
                [Beto] = new()
            });

        Assert.Equal(0, racha);
    }

    // Que hoy todavia no cumplan no debe borrar la racha: recien se corta cuando
    // pasa un dia completo sin que ambos cumplan.
    [Fact]
    public async Task Si_hoy_aun_no_cumplieron_cuenta_desde_ayer()
    {
        var dias = new[] { Hoy.AddDays(-1), Hoy.AddDays(-2) };

        var racha = await RachaEntreAsync(
            new() { [Ana] = new() { Recurrente(10, Ana) }, [Beto] = new() { Recurrente(20, Beto) } },
            new()
            {
                [Ana] = dias.Select(d => Completada(10, d)).ToList(),
                [Beto] = dias.Select(d => Completada(20, d)).ToList()
            });

        Assert.Equal(2, racha);
    }

    [Fact]
    public async Task Un_dia_salteado_corta_la_racha()
    {
        // Ambos cumplen hoy y ayer, pero anteayer no: la racha vale 2, no 3.
        var dias = new[] { Hoy, Hoy.AddDays(-1), Hoy.AddDays(-3) };

        var racha = await RachaEntreAsync(
            new() { [Ana] = new() { Recurrente(10, Ana) }, [Beto] = new() { Recurrente(20, Beto) } },
            new()
            {
                [Ana] = dias.Select(d => Completada(10, d)).ToList(),
                [Beto] = dias.Select(d => Completada(20, d)).ToList()
            });

        Assert.Equal(2, racha);
    }

    // Las tareas de un solo dia no viven en TaskCompletions: aportan su propia
    // ScheduledDate. Ambos criterios tienen que sumar al mismo conjunto de dias.
    [Fact]
    public async Task Las_tareas_de_un_solo_dia_tambien_cuentan()
    {
        var racha = await RachaEntreAsync(
            new()
            {
                [Ana] = new() { DeUnDia(11, Ana, Hoy) },
                [Beto] = new() { DeUnDia(21, Beto, Hoy) }
            },
            new() { [Ana] = new(), [Beto] = new() });

        Assert.Equal(1, racha);
    }

    [Fact]
    public async Task Mezcla_de_recurrente_y_de_un_dia_cuenta_igual()
    {
        var racha = await RachaEntreAsync(
            new()
            {
                [Ana] = new() { Recurrente(10, Ana) },
                [Beto] = new() { DeUnDia(21, Beto, Hoy) }
            },
            new()
            {
                [Ana] = new() { Completada(10, Hoy) },
                [Beto] = new()
            });

        Assert.Equal(1, racha);
    }
}
