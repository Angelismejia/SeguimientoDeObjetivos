using System.Security.Claims;
using Api.Controllers;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Services;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SeguimientoDeObjetivos.Tests;

// Message.ReadAt existia desde el principio pero nada lo llenaba: los "sin leer"
// del chat vivian solo en memoria del navegador y se perdian al recargar. Ahora
// abrir una conversacion marca como leidos los mensajes recibidos de esa persona.
public class MensajesLeidosTests
{
    private const int Yo = 1;
    private const int Ana = 2;
    private const int Beto = 3;

    private static Message Mensaje(int id, int de, int para) =>
        new() { Id = id, SenderId = de, ReceiverId = para, Content = "hola" };

    [Fact]
    public async Task Abrir_una_conversacion_marca_solo_lo_que_me_mando_esa_persona()
    {
        var deAna = Mensaje(1, Ana, Yo);
        var deBeto = Mensaje(2, Beto, Yo);
        var mioParaAna = Mensaje(3, Yo, Ana);
        var repo = new MessageRepoFalso(deAna, deBeto, mioParaAna);
        var servicio = new MessageService(repo, new UnitOfWorkContador());

        await servicio.MarkAsReadAsync(Yo, Ana);

        Assert.NotNull(deAna.ReadAt);
        Assert.Null(deBeto.ReadAt);
        // Lo que yo mande lo marca Ana cuando lo abra, no yo.
        Assert.Null(mioParaAna.ReadAt);
    }

    [Fact]
    public async Task Si_no_hay_nada_sin_leer_no_se_guarda_nada()
    {
        var unitOfWork = new UnitOfWorkContador();
        var servicio = new MessageService(new MessageRepoFalso(Mensaje(1, Beto, Yo)), unitOfWork);

        await servicio.MarkAsReadAsync(Yo, Ana);

        Assert.Equal(0, unitOfWork.Guardados);
    }

    [Fact]
    public async Task Los_que_ya_estaban_leidos_conservan_su_hora()
    {
        var antes = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        var viejo = Mensaje(1, Ana, Yo);
        viejo.ReadAt = antes;
        var servicio = new MessageService(new MessageRepoFalso(viejo, Mensaje(2, Ana, Yo)), new UnitOfWorkContador());

        await servicio.MarkAsReadAsync(Yo, Ana);

        Assert.Equal(antes, viejo.ReadAt);
    }

    [Fact]
    public async Task Los_remitentes_sin_leer_salen_una_sola_vez()
    {
        var repo = new MessageRepoFalso(Mensaje(1, Ana, Yo), Mensaje(2, Ana, Yo), Mensaje(3, Beto, Ana));
        var servicio = new MessageService(repo, new UnitOfWorkContador());

        var remitentes = await servicio.GetUnreadSenderIdsAsync(Yo);

        Assert.Equal(new[] { Ana }, remitentes);
    }

    // El endpoint no recibe el id del lector: lo saca del token. Aunque alguien
    // intente marcar una conversacion ajena, solo puede tocar sus propios mensajes.
    [Fact]
    public async Task El_endpoint_marca_como_lector_a_quien_trae_el_token()
    {
        var paraBeto = Mensaje(1, Ana, Beto);
        var paraMi = Mensaje(2, Ana, Yo);
        var servicio = new MessageService(new MessageRepoFalso(paraBeto, paraMi), new UnitOfWorkContador());
        var identidad = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, Yo.ToString()) }, "test");
        var controller = new MessagesController(servicio)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identidad) }
            }
        };

        var respuesta = await controller.MarkAsRead(Ana);

        Assert.IsType<NoContentResult>(respuesta);
        Assert.NotNull(paraMi.ReadAt);
        Assert.Null(paraBeto.ReadAt);
    }

    private sealed class UnitOfWorkContador : IUnitOfWork
    {
        public int Guardados { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            Guardados++;
            return Task.FromResult(0);
        }
    }

    private sealed class MessageRepoFalso : IMessageRepository
    {
        private readonly List<Message> _mensajes;

        public MessageRepoFalso(params Message[] mensajes) => _mensajes = mensajes.ToList();

        public Task<IEnumerable<int>> GetUnreadSenderIdsAsync(int receiverId) =>
            Task.FromResult<IEnumerable<int>>(_mensajes
                .Where(m => m.ReceiverId == receiverId && m.ReadAt == null)
                .Select(m => m.SenderId)
                .Distinct()
                .ToList());

        public Task<IEnumerable<Message>> GetUnreadFromAsync(int receiverId, int senderId) =>
            Task.FromResult<IEnumerable<Message>>(_mensajes
                .Where(m => m.ReceiverId == receiverId && m.SenderId == senderId && m.ReadAt == null)
                .ToList());

        public Task<IEnumerable<Message>> GetConversationAsync(int a, int b) => throw new NotSupportedException();
        public Task<IEnumerable<Message>> GetConversationPageAsync(int a, int b, int limit, int? beforeId) => throw new NotSupportedException();
        public Task<Message> CreateAsync(Message message) => throw new NotSupportedException();
        public Task DeleteAllForUserAsync(int userId) => throw new NotSupportedException();
    }
}
