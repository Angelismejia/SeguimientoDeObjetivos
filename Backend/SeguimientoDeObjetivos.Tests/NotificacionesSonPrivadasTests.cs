using System.Security.Claims;
using Api.Controllers;
using Application.DTOs.Notifications;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SeguimientoDeObjetivos.Tests;

// Las alertas de un usuario son privadas. Este controller tenia el mismo agujero
// que el diario: el userId llegaba por la URL y no se comparaba con el token, y
// el id de una notificacion suelta tampoco, asi que cambiando un numero se leian,
// marcaban y borraban las alertas de cualquiera.
//
// ControllersProtegenPropiedadTests no lo atrapo porque mira la FORMA del
// controller: le alcanzaba con que existiera la propiedad RequesterId, y existia
// (la usaba read-all). Estos tests miran el COMPORTAMIENTO: llaman a cada
// endpoint haciendose pasar por otro usuario y exigen un 403.
public class NotificacionesSonPrivadasTests
{
    private const int Yo = 1;
    private const int Otro = 2;

    private static NotificationDto Alerta(int id, int duenio) =>
        new() { Id = id, UserId = duenio, Title = "Insignia nueva", Message = "Ganaste una insignia" };

    private static NotificationsController ControllerComo(int userId, INotificationService servicio)
    {
        var identidad = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "test");

        return new NotificationsController(servicio)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identidad) }
            }
        };
    }

    [Fact]
    public async Task No_se_pueden_listar_las_alertas_de_otro()
    {
        var servicio = new NotificacionesFalsas(Alerta(10, Otro));
        var controller = ControllerComo(Yo, servicio);

        var respuesta = await controller.GetByUser(Otro);

        Assert.IsType<ForbidResult>(respuesta.Result);
        Assert.Empty(servicio.Llamadas);
    }

    [Fact]
    public async Task Las_alertas_propias_si_se_listan()
    {
        var servicio = new NotificacionesFalsas(Alerta(10, Yo));
        var controller = ControllerComo(Yo, servicio);

        var respuesta = await controller.GetByUser(Yo);

        var ok = Assert.IsType<OkObjectResult>(respuesta.Result);
        Assert.Single((IEnumerable<NotificationDto>)ok.Value!);
    }

    [Fact]
    public async Task No_se_pueden_listar_las_no_leidas_de_otro()
    {
        var servicio = new NotificacionesFalsas();
        var controller = ControllerComo(Yo, servicio);

        var respuesta = await controller.GetUnread(Otro);

        Assert.IsType<ForbidResult>(respuesta.Result);
        Assert.Empty(servicio.Llamadas);
    }

    [Fact]
    public async Task No_se_puede_leer_una_alerta_ajena_por_su_id()
    {
        var servicio = new NotificacionesFalsas(Alerta(10, Otro));
        var controller = ControllerComo(Yo, servicio);

        var respuesta = await controller.GetById(10);

        Assert.IsType<ForbidResult>(respuesta.Result);
    }

    // El 403 no alcanza: lo que importa es que la accion no llegue a ejecutarse.
    [Fact]
    public async Task No_se_puede_marcar_como_leida_una_alerta_ajena()
    {
        var servicio = new NotificacionesFalsas(Alerta(10, Otro));
        var controller = ControllerComo(Yo, servicio);

        var respuesta = await controller.MarkAsRead(10);

        Assert.IsType<ForbidResult>(respuesta);
        Assert.DoesNotContain("MarkAsRead(10)", servicio.Llamadas);
    }

    [Fact]
    public async Task No_se_puede_borrar_una_alerta_ajena()
    {
        var servicio = new NotificacionesFalsas(Alerta(10, Otro));
        var controller = ControllerComo(Yo, servicio);

        var respuesta = await controller.Delete(10);

        Assert.IsType<ForbidResult>(respuesta);
        Assert.DoesNotContain("Delete(10)", servicio.Llamadas);
    }

    [Fact]
    public async Task Las_alertas_propias_si_se_pueden_marcar_y_borrar()
    {
        var servicio = new NotificacionesFalsas(Alerta(10, Yo));
        var controller = ControllerComo(Yo, servicio);

        Assert.IsType<NoContentResult>(await controller.MarkAsRead(10));
        Assert.IsType<NoContentResult>(await controller.Delete(10));
        Assert.Contains("MarkAsRead(10)", servicio.Llamadas);
        Assert.Contains("Delete(10)", servicio.Llamadas);
    }

    // read-all nunca recibio el userId por parametro, y no debe volver a recibirlo.
    [Fact]
    public async Task Marcar_todas_usa_el_token_y_no_acepta_un_userId()
    {
        var servicio = new NotificacionesFalsas();
        var controller = ControllerComo(Yo, servicio);

        Assert.Empty(typeof(NotificationsController)
            .GetMethod(nameof(NotificationsController.MarkAllAsRead))!
            .GetParameters());

        Assert.IsType<NoContentResult>(await controller.MarkAllAsRead());
        Assert.Contains($"MarkAll({Yo})", servicio.Llamadas);
    }

    // El POST permitia fabricarle una alerta a cualquiera. Se quito; que no vuelva.
    [Fact]
    public void No_existe_un_endpoint_para_crear_alertas_a_mano()
    {
        var crea = typeof(NotificationsController)
            .GetMethods()
            .Where(m => m.DeclaringType == typeof(NotificationsController))
            .Any(m => m.GetParameters().Any(p => p.ParameterType == typeof(CreateNotificationDto)));

        Assert.False(crea,
            "NotificationsController no debe exponer la creacion de alertas: " +
            "permitiria mandarle una notificacion falsa a cualquier usuario.");
    }
}

internal sealed class NotificacionesFalsas : INotificationService
{
    private readonly Dictionary<int, NotificationDto> _porId;
    public NotificacionesFalsas(params NotificationDto[] alertas) =>
        _porId = alertas.ToDictionary(a => a.Id);

    public List<string> Llamadas { get; } = new();

    public Task<IEnumerable<NotificationDto>> GetByUserIdAsync(int userId)
    {
        Llamadas.Add($"GetByUser({userId})");
        return Task.FromResult(_porId.Values.Where(n => n.UserId == userId));
    }

    public Task<IEnumerable<NotificationDto>> GetUnreadByUserIdAsync(int userId)
    {
        Llamadas.Add($"GetUnread({userId})");
        return Task.FromResult(_porId.Values.Where(n => n.UserId == userId && !n.IsRead));
    }

    public Task<NotificationDto> GetByIdAsync(int id) => Task.FromResult(_porId[id]);

    public Task MarkAsReadAsync(int id)
    {
        Llamadas.Add($"MarkAsRead({id})");
        return Task.CompletedTask;
    }

    public Task<int> MarkAllAsReadAsync(int userId)
    {
        Llamadas.Add($"MarkAll({userId})");
        return Task.FromResult(0);
    }

    public Task DeleteAsync(int id)
    {
        Llamadas.Add($"Delete({id})");
        return Task.CompletedTask;
    }

    public Task<NotificationDto> CreateAsync(CreateNotificationDto dto) =>
        throw new NotSupportedException("El controller no debe crear alertas.");
}
