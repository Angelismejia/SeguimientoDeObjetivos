using System.IdentityModel.Tokens.Jwt;
using Application.Common;
using Application.Services;
using Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace SeguimientoDeObjetivos.Tests;

// Antes un JWT valia sus 24 horas pasara lo que pasara: cambiar la contraseña
// no cerraba la sesion de quien te la habia robado, y el token de una cuenta
// borrada o desactivada seguia entrando. Ahora cada token lleva el sello del hash
// de la contraseña y la API lo compara en cada peticion.
public class SelloDeSesionTests
{
    private static User Usuario(string clave = "clave-original", bool activo = true) => new()
    {
        Id = 7,
        Username = "ana",
        Email = "ana@ejemplo.com",
        IsActive = activo,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(clave)
    };

    private static string SelloDelToken(string token) =>
        new JwtSecurityTokenHandler().ReadJwtToken(token).Claims.Single(c => c.Type == SelloDeSesion.Claim).Value;

    [Fact]
    public async Task El_token_emitido_lleva_un_sello_vigente()
    {
        var usuario = Usuario();
        var token = await AuthCon(usuario).IssueTokenAsync(usuario.Id);

        Assert.True(SelloDeSesion.SigueVigente(usuario, SelloDelToken(token)));
    }

    [Fact]
    public async Task Cambiar_la_contrasena_invalida_los_tokens_anteriores()
    {
        var usuario = Usuario();
        var sello = SelloDelToken(await AuthCon(usuario).IssueTokenAsync(usuario.Id));

        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword("clave-nueva");

        Assert.False(SelloDeSesion.SigueVigente(usuario, sello));
    }

    [Fact]
    public async Task El_token_nuevo_despues_del_cambio_si_vale()
    {
        var usuario = Usuario();
        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword("clave-nueva");

        var token = await AuthCon(usuario).IssueTokenAsync(usuario.Id);

        Assert.True(SelloDeSesion.SigueVigente(usuario, SelloDelToken(token)));
    }

    [Fact]
    public void Una_cuenta_desactivada_o_borrada_no_tiene_sesion()
    {
        var desactivado = Usuario(activo: false);

        Assert.False(SelloDeSesion.SigueVigente(desactivado, SelloDeSesion.De(desactivado.PasswordHash)));
        Assert.False(SelloDeSesion.SigueVigente(null, "cualquiera"));
    }

    [Fact]
    public void Un_token_sin_sello_no_vale()
    {
        Assert.False(SelloDeSesion.SigueVigente(Usuario(), null));
    }

    private static AuthService AuthCon(User usuario)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Secret"] = new string('s', 64) })
            .Build();
        return new AuthService(new UnUsuarioFijo(usuario), null!, null!, config);
    }

    private sealed class UnUsuarioFijo : Application.Interfaces.Repositories.IUserRepository
    {
        private readonly User _usuario;
        public UnUsuarioFijo(User usuario) => _usuario = usuario;

        public Task<User?> GetByIdAsync(int id) => Task.FromResult<User?>(id == _usuario.Id ? _usuario : null);

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
}
