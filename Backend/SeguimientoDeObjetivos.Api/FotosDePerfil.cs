namespace Api
{
    /// <summary>
    /// Reglas de las fotos de perfil. Antes solo se miraba la extension del nombre,
    /// asi que cualquier archivo renombrado a .png entraba, no habia mas tope de
    /// tamaño que el de ASP.NET Core (~28 MB), y cada foto nueva dejaba la anterior
    /// en el disco para siempre (el del plan gratuito de Azure es chico).
    /// </summary>
    public static class FotosDePerfil
    {
        // Una foto de celular sin comprimir anda por los 3-5 MB.
        public const long TamanoMaximo = 5 * 1024 * 1024;

        public static readonly string[] Extensiones = { ".jpg", ".jpeg", ".png", ".webp" };

        /// <summary>
        /// Si los primeros bytes son de verdad de una imagen del formato que dice la
        /// extension. La extension la elige quien sube el archivo; la cabecera no.
        /// </summary>
        public static bool CabeceraCoincide(ReadOnlySpan<byte> cabecera, string extension) => extension switch
        {
            ".jpg" or ".jpeg" => cabecera.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF }),
            ".png" => cabecera.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            // RIFF <4 bytes de tamaño> WEBP
            ".webp" => cabecera.Length >= 12
                && cabecera[..4].SequenceEqual("RIFF"u8)
                && cabecera[8..12].SequenceEqual("WEBP"u8),
            _ => false
        };

        /// <summary>
        /// Borra del disco una foto que subimos nosotros. Ignora cualquier URL que no
        /// sea /uploads/&lt;nombre&gt; para que un valor raro en la base no pueda
        /// apuntar fuera de la carpeta.
        /// </summary>
        public static void Borrar(string? photoUrl, string carpeta)
        {
            if (string.IsNullOrEmpty(photoUrl) || !photoUrl.StartsWith("/uploads/")) return;

            var nombre = Path.GetFileName(photoUrl);
            if (nombre != photoUrl["/uploads/".Length..]) return;

            var ruta = Path.Combine(carpeta, nombre);
            try
            {
                if (File.Exists(ruta)) File.Delete(ruta);
            }
            catch (IOException)
            {
                // Que no se pueda borrar la vieja no debe hacer fallar la subida.
            }
        }
    }
}
