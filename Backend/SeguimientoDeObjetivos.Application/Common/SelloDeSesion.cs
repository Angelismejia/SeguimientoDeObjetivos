using Domain.Entities;

namespace Application.Common
{
    /// <summary>
    /// Ata cada JWT a la contraseña con la que se emitio. El token lleva la huella
    /// del hash actual en el claim <see cref="Claim"/>, y la API la vuelve a
    /// comparar en cada peticion (ver OnTokenValidated en Program.cs).
    ///
    /// Sin esto, el token valia sus 24 horas pasara lo que pasara: si a alguien le
    /// robaban la sesion y cambiaba la contraseña para protegerse, el ladron seguia
    /// entrando. Ahora cambiar o restablecer la contraseña cambia el hash, la huella
    /// deja de coincidir y todas las sesiones anteriores quedan cerradas.
    ///
    /// Es la misma idea que hace de un solo uso el enlace de recuperacion, y por la
    /// misma razon no necesita columna nueva (ni migracion).
    /// </summary>
    public static class SelloDeSesion
    {
        public const string Claim = "sello";

        public static string De(string passwordHash) => EnlaceDeRecuperacion.Huella(passwordHash);

        /// <summary>
        /// Si un token con este sello sigue valiendo para este usuario: la cuenta
        /// existe, esta activa y la contraseña es la misma que cuando se emitio.
        /// </summary>
        public static bool SigueVigente(User? user, string? sello)
            => user is not null
               && user.IsActive
               && sello is not null
               && De(user.PasswordHash) == sello;
    }
}
