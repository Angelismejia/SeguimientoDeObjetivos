using Application.Common;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Services;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.Extensions.Configuration;

namespace SeguimientoDeObjetivos.Tests;

// "Olvide mi contraseña". El token del enlace no se guarda en la base: va
// firmado y lleva una huella de la contraseña actual, que es lo que lo vuelve
// de un solo uso.
public class RecuperarContrasenaTests
{
    private const string Secreto = "secreto-de-prueba-con-largo-suficiente-1234567890";
    private static readonly DateTime Ahora = new(2026, 11, 5, 15, 0, 0, DateTimeKind.Utc);

    private sealed class Escenario
    {
        public User Usuario { get; } = new()
        {
            Id = 7, Name = "Ana", Username = "ana", Email = "ana@correo.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("vieja123"), IsActive = true
        };
        public CorreosAnotados Correos { get; } = new();
        public DateTime Reloj { get; set; } = Ahora;
        public PasswordResetService Servicio { get; }

        public Escenario()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = Secreto,
                ["App:PublicUrl"] = "https://app.test"
            }).Build();
            Servicio = new PasswordResetService(new UnUsuario(Usuario), new UnitOfWorkFalso(), Correos, config, () => Reloj);
        }

        public string TokenDelCorreo()
        {
            var html = Assert.Single(Correos.Enviados).Html;
            var desde = html.IndexOf("token=", StringComparison.Ordinal) + "token=".Length;
            return Uri.UnescapeDataString(html[desde..html.IndexOf('"', desde)]);
        }
    }

    [Fact]
    public async Task Con_el_enlace_del_correo_se_cambia_la_contrasena()
    {
        var e = new Escenario();

        await e.Servicio.RequestAsync("ana@correo.com");
        await e.Servicio.ResetAsync(e.TokenDelCorreo(), "nueva456");

        Assert.True(BCrypt.Net.BCrypt.Verify("nueva456", e.Usuario.PasswordHash));
        Assert.Contains("https://app.test/reset-password?token=", e.Correos.Enviados[0].Html);
    }

    // Nunca dice si el correo existe; simplemente no manda nada.
    [Fact]
    public async Task Un_correo_desconocido_no_falla_ni_manda_nada()
    {
        var e = new Escenario();

        await e.Servicio.RequestAsync("nadie@correo.com");

        Assert.Empty(e.Correos.Enviados);
    }

    [Fact]
    public async Task El_enlace_sirve_una_sola_vez()
    {
        var e = new Escenario();
        await e.Servicio.RequestAsync("ana@correo.com");
        var token = e.TokenDelCorreo();

        await e.Servicio.ResetAsync(token, "nueva456");

        await Assert.ThrowsAsync<BusinessRuleException>(() => e.Servicio.ResetAsync(token, "otra789"));
        Assert.True(BCrypt.Net.BCrypt.Verify("nueva456", e.Usuario.PasswordHash));
    }

    [Fact]
    public async Task Pasada_la_hora_el_enlace_vence()
    {
        var e = new Escenario();
        await e.Servicio.RequestAsync("ana@correo.com");

        e.Reloj = Ahora.AddMinutes(61);

        await Assert.ThrowsAsync<BusinessRuleException>(() => e.Servicio.ResetAsync(e.TokenDelCorreo(), "nueva456"));
    }

    [Fact]
    public void Un_token_con_el_usuario_cambiado_no_pasa_la_firma()
    {
        var token = EnlaceDeRecuperacion.Crear(7, "hash", Ahora.AddHours(1), Secreto);
        var datos = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(
            token.Split('.')[0].Replace('-', '+').Replace('_', '/').PadRight(((token.Split('.')[0].Length + 3) / 4) * 4, '=')));
        var falso = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("1" + datos[1..]))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_') + "." + token.Split('.')[1];

        Assert.NotNull(EnlaceDeRecuperacion.Leer(token, Secreto, Ahora));
        Assert.Null(EnlaceDeRecuperacion.Leer(falso, Secreto, Ahora));
    }

    [Fact]
    public void Un_token_firmado_con_otro_secreto_no_vale()
    {
        var token = EnlaceDeRecuperacion.Crear(7, "hash", Ahora.AddHours(1), "otro-secreto");

        Assert.Null(EnlaceDeRecuperacion.Leer(token, Secreto, Ahora));
    }

    [Theory]
    [InlineData("")]
    [InlineData("basura")]
    [InlineData("a.b.c")]
    [InlineData("%%%.###")]
    public void Un_token_mal_formado_no_explota(string token)
    {
        Assert.Null(EnlaceDeRecuperacion.Leer(token, Secreto, Ahora));
    }
}

internal sealed class CorreosAnotados : IEmailSender
{
    public List<(string To, string Subject, string Html)> Enviados { get; } = new();

    public Task SendAsync(string to, string subject, string html, CancellationToken ct = default)
    {
        Enviados.Add((to, subject, html));
        return Task.CompletedTask;
    }
}

internal sealed class UnUsuario : IUserRepository
{
    private readonly User _user;
    public UnUsuario(User user) => _user = user;

    public Task<User?> GetByIdAsync(int id) => Task.FromResult(id == _user.Id ? _user : null);
    public Task<User?> GetByEmailAsync(string email) => Task.FromResult(email == _user.Email ? _user : null);
    public Task<User> UpdateAsync(User user) => Task.FromResult(user);

    public Task<IEnumerable<User>> GetAllAsync() => throw new NotSupportedException();
    public Task<IEnumerable<User>> SearchActiveAsync(string? query, int limit) => throw new NotSupportedException();
    public Task<User?> GetByUsernameAsync(string username) => throw new NotSupportedException();
    public void Add(User user) => throw new NotSupportedException();
    public Task<User> CreateAsync(User user) => throw new NotSupportedException();
    public Task<bool> DeleteAsync(int id) => throw new NotSupportedException();
    public Task<bool> ExistsAsync(int id) => throw new NotSupportedException();
}
