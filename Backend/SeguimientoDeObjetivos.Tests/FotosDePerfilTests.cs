using System.Text;
using Api;

namespace SeguimientoDeObjetivos.Tests;

// Antes solo se miraba la extension del nombre: cualquier archivo renombrado a
// .png entraba, y cada foto nueva dejaba la anterior en el disco para siempre.
public class FotosDePerfilTests
{
    private static readonly byte[] Jpg = { 0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10, 0x4A, 0x46, 0x49, 0x46, 0, 1 };
    private static readonly byte[] Png = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D };
    private static readonly byte[] Webp = Encoding.ASCII.GetBytes("RIFF\u0024\0\0\0WEBP");

    [Theory]
    [InlineData(".jpg")]
    [InlineData(".jpeg")]
    public void Un_jpg_de_verdad_pasa(string extension) =>
        Assert.True(FotosDePerfil.CabeceraCoincide(Jpg, extension));

    [Fact]
    public void Un_png_y_un_webp_de_verdad_pasan()
    {
        Assert.True(FotosDePerfil.CabeceraCoincide(Png, ".png"));
        Assert.True(FotosDePerfil.CabeceraCoincide(Webp, ".webp"));
    }

    [Fact]
    public void Un_archivo_cualquiera_renombrado_a_png_no_pasa()
    {
        var html = Encoding.ASCII.GetBytes("<html><script>");
        Assert.False(FotosDePerfil.CabeceraCoincide(html, ".png"));
    }

    [Fact]
    public void La_cabecera_tiene_que_coincidir_con_la_extension()
    {
        Assert.False(FotosDePerfil.CabeceraCoincide(Png, ".jpg"));
        Assert.False(FotosDePerfil.CabeceraCoincide(Jpg, ".webp"));
    }

    [Fact]
    public void Un_archivo_mas_corto_que_la_cabecera_no_pasa() =>
        Assert.False(FotosDePerfil.CabeceraCoincide(new byte[] { 0x89, 0x50 }, ".png"));

    [Fact]
    public void Borrar_quita_la_foto_vieja_de_la_carpeta()
    {
        var carpeta = Directory.CreateTempSubdirectory().FullName;
        var vieja = Path.Combine(carpeta, "user1-vieja.png");
        File.WriteAllBytes(vieja, Png);

        FotosDePerfil.Borrar("/uploads/user1-vieja.png", carpeta);

        Assert.False(File.Exists(vieja));
    }

    [Theory]
    [InlineData("/uploads/../secreto.txt")]
    [InlineData("/uploads/sub/secreto.txt")]
    [InlineData("https://otro-sitio.com/secreto.txt")]
    public void Borrar_no_sale_de_la_carpeta_aunque_la_url_sea_rara(string url)
    {
        var raiz = Directory.CreateTempSubdirectory().FullName;
        var carpeta = Directory.CreateDirectory(Path.Combine(raiz, "uploads")).FullName;
        Directory.CreateDirectory(Path.Combine(carpeta, "sub"));
        var fuera = Path.Combine(raiz, "secreto.txt");
        var adentro = Path.Combine(carpeta, "sub", "secreto.txt");
        File.WriteAllText(fuera, "x");
        File.WriteAllText(adentro, "x");

        FotosDePerfil.Borrar(url, carpeta);

        Assert.True(File.Exists(fuera));
        Assert.True(File.Exists(adentro));
    }
}
