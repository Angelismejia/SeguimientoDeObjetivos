using Application.DTOs.Messages;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Services;
using Domain.Entities;
using Domain.Exceptions;

namespace SeguimientoDeObjetivos.Tests;

// El chat entra por SignalR, que no corre las validaciones de los DTOs: el
// [MaxLength(2000)] de CreateMessageDto solo lo aplicaba [ApiController]. Por eso
// las reglas viven en MessageService.SendAsync y se prueban ahi.
public class ChatValidaLosMensajesTests
{
    private const int Yo = 1;
    private const int Ana = 2;      // la sigo
    private const int Beto = 3;     // me sigue
    private const int Extrano = 4;  // ni me sigue ni lo sigo
    private const int Inactivo = 5; // la sigo, pero desactivo su cuenta

    private static (MessageService Servicio, MensajesGuardados Repo) Crear()
    {
        var usuarios = new UsuariosFijos(
            new User { Id = Yo, IsActive = true },
            new User { Id = Ana, IsActive = true },
            new User { Id = Beto, IsActive = true },
            new User { Id = Extrano, IsActive = true },
            new User { Id = Inactivo, IsActive = false });
        var seguimientos = new SeguimientosFijos(
            new Follow { FollowerId = Yo, FollowingId = Ana },
            new Follow { FollowerId = Beto, FollowingId = Yo },
            new Follow { FollowerId = Yo, FollowingId = Inactivo });
        var repo = new MensajesGuardados();
        return (new MessageService(repo, usuarios, seguimientos, new SinGuardar()), repo);
    }

    private static CreateMessageDto Para(int receptor, string texto = "hola") =>
        new() { ReceiverId = receptor, Content = texto };

    [Theory]
    [InlineData(Ana)]   // yo la sigo
    [InlineData(Beto)]  // el me sigue
    public async Task Se_puede_escribir_si_uno_de_los_dos_sigue_al_otro(int receptor)
    {
        var (servicio, repo) = Crear();

        var enviado = await servicio.SendAsync(Yo, Para(receptor));

        Assert.Equal(receptor, enviado.ReceiverId);
        Assert.Single(repo.Mensajes);
    }

    [Fact]
    public async Task No_se_puede_escribir_a_un_desconocido()
    {
        var (servicio, repo) = Crear();

        await Assert.ThrowsAsync<BusinessRuleException>(() => servicio.SendAsync(Yo, Para(Extrano)));
        Assert.Empty(repo.Mensajes);
    }

    [Theory]
    [InlineData(999)]       // no existe: antes reventaba con un error de clave foranea
    [InlineData(Inactivo)]  // cuenta desactivada
    public async Task No_se_puede_escribir_a_quien_no_esta_disponible(int receptor)
    {
        var (servicio, repo) = Crear();

        await Assert.ThrowsAsync<BusinessRuleException>(() => servicio.SendAsync(Yo, Para(receptor)));
        Assert.Empty(repo.Mensajes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Un_mensaje_vacio_se_rechaza(string texto)
    {
        var (servicio, repo) = Crear();

        await Assert.ThrowsAsync<BusinessRuleException>(() => servicio.SendAsync(Yo, Para(Ana, texto)));
        Assert.Empty(repo.Mensajes);
    }

    [Fact]
    public async Task Un_mensaje_de_mas_de_2000_caracteres_se_rechaza()
    {
        var (servicio, repo) = Crear();

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => servicio.SendAsync(Yo, Para(Ana, new string('a', MessageService.LargoMaximo + 1))));
        Assert.Empty(repo.Mensajes);
    }

    [Fact]
    public async Task Justo_2000_caracteres_si_pasa_y_se_guarda_sin_espacios_de_los_bordes()
    {
        var (servicio, _) = Crear();
        var texto = new string('a', MessageService.LargoMaximo);

        var enviado = await servicio.SendAsync(Yo, Para(Ana, $"  {texto}  "));

        Assert.Equal(texto, enviado.Content);
    }

    [Fact]
    public async Task No_se_puede_escribir_a_uno_mismo()
    {
        var (servicio, _) = Crear();

        await Assert.ThrowsAsync<BusinessRuleException>(() => servicio.SendAsync(Yo, Para(Yo)));
    }

    private sealed class SinGuardar : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(0);
    }

    private sealed class MensajesGuardados : IMessageRepository
    {
        public List<Message> Mensajes { get; } = new();

        public Task<Message> CreateAsync(Message message)
        {
            Mensajes.Add(message);
            return Task.FromResult(message);
        }

        public Task<IEnumerable<int>> GetUnreadSenderIdsAsync(int receiverId) => throw new NotSupportedException();
        public Task<IEnumerable<Message>> GetUnreadFromAsync(int receiverId, int senderId) => throw new NotSupportedException();
        public Task<IEnumerable<Message>> GetConversationAsync(int a, int b) => throw new NotSupportedException();
        public Task<IEnumerable<Message>> GetConversationPageAsync(int a, int b, int limit, int? beforeId) => throw new NotSupportedException();
        public Task DeleteAllForUserAsync(int userId) => throw new NotSupportedException();
    }

    private sealed class UsuariosFijos : IUserRepository
    {
        private readonly Dictionary<int, User> _usuarios;
        public UsuariosFijos(params User[] usuarios) => _usuarios = usuarios.ToDictionary(u => u.Id);

        public Task<User?> GetByIdAsync(int id) => Task.FromResult(_usuarios.GetValueOrDefault(id));

        public Task<IEnumerable<User>> GetAllAsync() => throw new NotSupportedException();
        public Task<IEnumerable<User>> SearchActiveAsync(string? query, int limit) => throw new NotSupportedException();
        public Task<User?> GetByUsernameAsync(string username) => throw new NotSupportedException();
        public Task<User?> GetByEmailAsync(string email) => throw new NotSupportedException();
        public void Add(User user) => throw new NotSupportedException();
        public Task<User> CreateAsync(User user) => throw new NotSupportedException();
        public Task<User> UpdateAsync(User user) => throw new NotSupportedException();
        public Task<bool> DeleteAsync(int id) => throw new NotSupportedException();
        public Task<bool> ExistsAsync(int id) => throw new NotSupportedException();
    }

    private sealed class SeguimientosFijos : IFollowRepository
    {
        private readonly List<Follow> _follows;
        public SeguimientosFijos(params Follow[] follows) => _follows = follows.ToList();

        public Task<Follow?> GetAsync(int followerId, int followingId) =>
            Task.FromResult(_follows.FirstOrDefault(f => f.FollowerId == followerId && f.FollowingId == followingId));

        public Task<Follow?> GetByIdAsync(int id) => throw new NotSupportedException();
        public Task<IEnumerable<Follow>> GetFollowersAsync(int userId) => throw new NotSupportedException();
        public Task<IEnumerable<Follow>> GetFollowingAsync(int userId) => throw new NotSupportedException();
        public Task<Follow> CreateAsync(Follow follow) => throw new NotSupportedException();
        public Task<bool> DeleteAsync(int a, int b) => throw new NotSupportedException();
        public Task DeleteAllForUserAsync(int userId) => throw new NotSupportedException();
    }
}
