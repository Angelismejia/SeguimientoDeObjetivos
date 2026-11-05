using System.Security.Cryptography;
using System.Text;

namespace Application.Common
{
    /// <summary>
    /// El token del enlace "olvidé mi contraseña". No se guarda en la base: va
    /// firmado con HMAC, asi que nadie puede fabricar uno ni cambiarle el usuario
    /// o la fecha sin que la firma deje de coincidir.
    ///
    /// <para>
    /// Lleva tres cosas: el id del usuario, hasta cuando vale y una HUELLA de su
    /// hash de contraseña actual. La huella es lo que lo hace de un solo uso: al
    /// restablecer la contraseña el hash cambia, la huella ya no coincide y el
    /// mismo enlace deja de servir. Tambien muere si el usuario cambia la
    /// contraseña por otro lado mientras tanto.
    /// </para>
    /// </summary>
    public static class EnlaceDeRecuperacion
    {
        public static readonly TimeSpan Vigencia = TimeSpan.FromHours(1);

        public static string Crear(int userId, string passwordHash, DateTime expiraUtc, string secreto)
        {
            var datos = $"{userId}.{new DateTimeOffset(expiraUtc, TimeSpan.Zero).ToUnixTimeSeconds()}.{Huella(passwordHash)}";
            return $"{Base64Url(Encoding.UTF8.GetBytes(datos))}.{Base64Url(Firmar(datos, secreto))}";
        }

        /// <summary>
        /// El id del usuario si el token esta bien firmado y no vencio; null si no.
        /// La huella se devuelve aparte porque compararla exige traer al usuario.
        /// </summary>
        public static (int UserId, string Huella)? Leer(string token, string secreto, DateTime ahoraUtc)
        {
            var partes = (token ?? string.Empty).Split('.');
            if (partes.Length != 2) return null;

            string datos;
            byte[] firma;
            try
            {
                datos = Encoding.UTF8.GetString(DesdeBase64Url(partes[0]));
                firma = DesdeBase64Url(partes[1]);
            }
            catch (FormatException)
            {
                return null;
            }

            // Comparacion en tiempo constante: una comparacion normal corta en el
            // primer byte distinto, y medir cuanto tarda ayudaria a adivinar la firma.
            if (!CryptographicOperations.FixedTimeEquals(firma, Firmar(datos, secreto))) return null;

            var campos = datos.Split('.');
            if (campos.Length != 3
                || !int.TryParse(campos[0], out var userId)
                || !long.TryParse(campos[1], out var expira)) return null;

            if (DateTimeOffset.FromUnixTimeSeconds(expira).UtcDateTime < ahoraUtc) return null;

            return (userId, campos[2]);
        }

        public static string Huella(string passwordHash)
            => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(passwordHash)))[..16];

        // La clave sale del secreto del JWT pero con un proposito propio, para que
        // una firma de este token no sirva como firma de ninguna otra cosa.
        private static byte[] Firmar(string datos, string secreto)
        {
            var clave = SHA256.HashData(Encoding.UTF8.GetBytes("recuperar-contrasena:" + secreto));
            return HMACSHA256.HashData(clave, Encoding.UTF8.GetBytes(datos));
        }

        private static string Base64Url(byte[] bytes)
            => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private static byte[] DesdeBase64Url(string texto)
        {
            var b64 = texto.Replace('-', '+').Replace('_', '/');
            return Convert.FromBase64String(b64.PadRight(b64.Length + (4 - b64.Length % 4) % 4, '='));
        }
    }
}
