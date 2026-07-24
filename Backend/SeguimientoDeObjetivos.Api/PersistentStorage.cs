namespace Api
{
    // Los archivos que suben los usuarios (fotos de perfil) no pueden vivir en
    // wwwroot: esa carpeta se reemplaza entera en cada despliegue porque ahí
    // también va el build del frontend. HOME (D:\home en Azure) sí persiste
    // entre despliegues, así que guardamos ahí en su lugar.
    public static class PersistentStorage
    {
        public static string UploadsPath(IWebHostEnvironment env)
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            var root = string.IsNullOrEmpty(home) ? env.ContentRootPath : home;
            var path = Path.Combine(root, "data", "uploads");
            Directory.CreateDirectory(path);
            return path;
        }
    }
}
