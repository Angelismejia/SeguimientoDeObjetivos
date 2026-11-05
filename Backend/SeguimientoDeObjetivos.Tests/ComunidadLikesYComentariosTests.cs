using System.Security.Claims;
using Api.Controllers;
using Application.DTOs.Notifications;
using Application.DTOs.Objectives;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SeguimientoDeObjetivos.Tests;

// Likes y comentarios en Comunidad. Se arma el controller con los servicios
// REALES y solo los repositorios falsos, asi se prueba la regla completa: quien
// puede ver el objetivo (controller), que este logrado (servicio) y a quien le
// llega la alerta.
public class ComunidadLikesYComentariosTests
{
    private const int Duenio = 1;
    private const int Amigo = 2;
    private const int Extranio = 3;

    private static Objective Objetivo(ObjectiveStatus estado = ObjectiveStatus.Completed, bool privado = false) =>
        new() { Id = 50, UserId = Duenio, Title = "Correr 10K", Status = estado, IsPrivate = privado };

    private sealed class Escenario
    {
        public SocialRepoEnMemoria Social { get; } = new();
        public AlertasAnotadas Alertas { get; } = new();
        public ObjectivesController Como(int userId) => _armar(userId);
        private readonly Func<int, ObjectivesController> _armar;

        public Escenario(Objective objetivo)
        {
            // El amigo sigue al dueño; el extraño no sigue a nadie.
            var follows = new SeguidosFijos(new Follow { FollowerId = Amigo, FollowingId = Duenio });
            var usuarios = new UserRepoFalso();
            var uow = new UnitOfWorkFalso();

            var objetivos = new ObjectiveService(new ObjectiveRepoFalso(objetivo), uow,
                new BadgeAwardFalso(), Alertas, follows, usuarios);
            var seguir = new FollowService(follows, usuarios, uow, Alertas);
            var social = new ObjectiveSocialService(Social, new ObjectiveRepoFalso(objetivo), usuarios, Alertas, uow, follows);

            _armar = userId => new ObjectivesController(objetivos, seguir, social)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(
                            new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "test"))
                    }
                }
            };
        }
    }

    [Fact]
    public async Task Un_amigo_le_da_like_a_un_logro_y_al_duenio_le_llega_la_alerta()
    {
        var e = new Escenario(Objetivo());

        var respuesta = await e.Como(Amigo).Like(50);

        var resumen = (ObjectiveSocialDto)Assert.IsType<OkObjectResult>(respuesta.Result).Value!;
        Assert.Equal(1, resumen.LikeCount);
        Assert.True(resumen.LikedByMe);
        var alerta = Assert.Single(e.Alertas.Creadas);
        Assert.Equal(Duenio, alerta.UserId);
        Assert.Equal("objective_liked", alerta.Type);
    }

    // Un doble click no duplica el like ni la alerta.
    [Fact]
    public async Task Dar_like_dos_veces_cuenta_uno_solo()
    {
        var e = new Escenario(Objetivo());

        await e.Como(Amigo).Like(50);
        await e.Como(Amigo).Like(50);

        Assert.Single(e.Social.Likes);
        Assert.Single(e.Alertas.Creadas);
    }

    [Fact]
    public async Task Quien_no_sigue_al_duenio_no_puede_dar_like()
    {
        var e = new Escenario(Objetivo());

        var respuesta = await e.Como(Extranio).Like(50);

        Assert.IsType<ForbidResult>(respuesta.Result);
        Assert.Empty(e.Social.Likes);
        Assert.Empty(e.Alertas.Creadas);
    }

    [Fact]
    public async Task Un_objetivo_privado_no_se_puede_comentar_aunque_lo_sigas()
    {
        var e = new Escenario(Objetivo(privado: true));

        var respuesta = await e.Como(Amigo).AddComment(50, new CreateObjectiveCommentDto { Content = "¡Bien!" });

        Assert.IsType<ForbidResult>(respuesta.Result);
        Assert.Empty(e.Social.Comentarios);
    }

    [Fact]
    public async Task Un_objetivo_que_todavia_no_se_logro_no_admite_likes()
    {
        var e = new Escenario(Objetivo(ObjectiveStatus.InProgress));

        await Assert.ThrowsAsync<BusinessRuleException>(() => e.Como(Amigo).Like(50));
        Assert.Empty(e.Social.Likes);
    }

    [Fact]
    public async Task Al_comentar_al_duenio_le_llega_la_alerta_con_el_texto()
    {
        var e = new Escenario(Objetivo());

        var respuesta = await e.Como(Amigo).AddComment(50, new CreateObjectiveCommentDto { Content = "  ¡Felicidades!  " });

        var comentario = (ObjectiveCommentDto)Assert.IsType<OkObjectResult>(respuesta.Result).Value!;
        Assert.Equal("¡Felicidades!", comentario.Content);
        var alerta = Assert.Single(e.Alertas.Creadas);
        Assert.Equal(Duenio, alerta.UserId);
        Assert.Equal("objective_comment", alerta.Type);
        Assert.Contains("¡Felicidades!", alerta.Message);
    }

    // El dueño puede contestar en su propio logro, pero no se manda una alerta a si mismo.
    [Fact]
    public async Task El_duenio_puede_comentar_lo_suyo_sin_alertarse()
    {
        var e = new Escenario(Objetivo());

        await e.Como(Duenio).AddComment(50, new CreateObjectiveCommentDto { Content = "¡Gracias!" });

        Assert.Single(e.Social.Comentarios);
        Assert.Empty(e.Alertas.Creadas);
    }

    [Fact]
    public async Task Un_comentario_ajeno_no_lo_borra_un_tercero_pero_si_el_duenio_del_objetivo()
    {
        var e = new Escenario(Objetivo());
        e.Social.AddComment(new ObjectiveComment { Id = 7, ObjectiveId = 50, UserId = Amigo, Content = "Hola" });

        Assert.IsType<ForbidResult>(await e.Como(Extranio).DeleteComment(7));
        Assert.Single(e.Social.Comentarios);

        Assert.IsType<NoContentResult>(await e.Como(Duenio).DeleteComment(7));
        Assert.Empty(e.Social.Comentarios);
    }

    // ── Aviso a los seguidores al lograr un objetivo ───────────

    private static UpdateObjectiveDto Completar(Objective o) =>
        new() { Title = o.Title, Status = ObjectiveStatus.Completed, ProgressPercentage = 100 };

    [Fact]
    public async Task Al_lograr_un_objetivo_publico_se_avisa_a_los_seguidores()
    {
        var objetivo = Objetivo(ObjectiveStatus.InProgress);
        var e = new Escenario(objetivo);

        await e.Como(Duenio).Update(50, Completar(objetivo));

        Assert.Contains(e.Alertas.Creadas, a => a.UserId == Amigo && a.Type == "friend_objective_completed");
    }

    [Fact]
    public async Task Al_lograr_un_objetivo_privado_no_se_avisa_a_nadie_mas()
    {
        var objetivo = Objetivo(ObjectiveStatus.InProgress, privado: true);
        var e = new Escenario(objetivo);

        await e.Como(Duenio).Update(50, Completar(objetivo));

        Assert.All(e.Alertas.Creadas, a => Assert.Equal(Duenio, a.UserId));
    }
}

internal sealed class SeguidosFijos : IFollowRepository
{
    private readonly List<Follow> _follows;
    public SeguidosFijos(params Follow[] follows) => _follows = follows.ToList();

    public Task<Follow?> GetAsync(int a, int b) =>
        Task.FromResult(_follows.FirstOrDefault(f => f.FollowerId == a && f.FollowingId == b));
    public Task<IEnumerable<Follow>> GetFollowersAsync(int userId) =>
        Task.FromResult<IEnumerable<Follow>>(_follows.Where(f => f.FollowingId == userId).ToList());

    public Task<Follow?> GetByIdAsync(int id) => throw new NotSupportedException();
    public Task<IEnumerable<Follow>> GetFollowingAsync(int userId) =>
        Task.FromResult<IEnumerable<Follow>>(_follows.Where(f => f.FollowerId == userId).ToList());
    public Task<Follow> CreateAsync(Follow follow) => throw new NotSupportedException();
    public Task<bool> DeleteAsync(int a, int b) => throw new NotSupportedException();
    public Task DeleteAllForUserAsync(int userId) => throw new NotSupportedException();
}

internal sealed class SocialRepoEnMemoria : IObjectiveSocialRepository
{
    public List<ObjectiveLike> Likes { get; } = new();
    public List<ObjectiveComment> Comentarios { get; } = new();

    public Task<ObjectiveLike?> GetLikeAsync(int objectiveId, int userId) =>
        Task.FromResult(Likes.FirstOrDefault(l => l.ObjectiveId == objectiveId && l.UserId == userId));
    public Task<IEnumerable<ObjectiveLike>> GetLikesAsync(IEnumerable<int> ids) =>
        Task.FromResult<IEnumerable<ObjectiveLike>>(Likes.Where(l => ids.Contains(l.ObjectiveId)).ToList());
    public void AddLike(ObjectiveLike like) => Likes.Add(like);
    public void RemoveLike(ObjectiveLike like) => Likes.Remove(like);

    public Task<IEnumerable<ObjectiveComment>> GetCommentsAsync(int objectiveId) =>
        Task.FromResult<IEnumerable<ObjectiveComment>>(Comentarios.Where(c => c.ObjectiveId == objectiveId).ToList());
    public Task<Dictionary<int, int>> CountCommentsAsync(IEnumerable<int> ids) =>
        Task.FromResult(Comentarios.Where(c => ids.Contains(c.ObjectiveId))
            .GroupBy(c => c.ObjectiveId).ToDictionary(g => g.Key, g => g.Count()));
    public Task<ObjectiveComment?> GetCommentAsync(int id) =>
        Task.FromResult(Comentarios.FirstOrDefault(c => c.Id == id));
    public void AddComment(ObjectiveComment comment) => Comentarios.Add(comment);
    public void RemoveComment(ObjectiveComment comment) => Comentarios.Remove(comment);
    public Task<IEnumerable<ObjectiveComment>> GetCommentsByUserAsync(int userId) =>
        Task.FromResult<IEnumerable<ObjectiveComment>>(Comentarios.Where(c => c.UserId == userId).ToList());
    public Task<IEnumerable<int>> GetLikedObjectiveIdsAsync(int userId) =>
        Task.FromResult<IEnumerable<int>>(Likes.Where(l => l.UserId == userId).Select(l => l.ObjectiveId).ToList());
}

internal sealed class AlertasAnotadas : INotificationService
{
    public List<CreateNotificationDto> Creadas { get; } = new();

    public Task<NotificationDto> CreateAsync(CreateNotificationDto dto)
    {
        Creadas.Add(dto);
        return Task.FromResult(new NotificationDto());
    }

    public Task<IEnumerable<NotificationDto>> GetByUserIdAsync(int userId) => throw new NotSupportedException();
    public Task<IEnumerable<NotificationDto>> GetUnreadByUserIdAsync(int userId) => throw new NotSupportedException();
    public Task<IEnumerable<NotificationDto>> GetPageByUserIdAsync(int userId, int limit, int? beforeId) => throw new NotSupportedException();
    public Task<int> CountUnreadAsync(int userId) => throw new NotSupportedException();
    public Task<NotificationDto> GetByIdAsync(int id) => throw new NotSupportedException();
    public Task MarkAsReadAsync(int id) => throw new NotSupportedException();
    public Task<int> MarkAllAsReadAsync(int userId) => throw new NotSupportedException();
    public Task DeleteAsync(int id) => throw new NotSupportedException();
}
