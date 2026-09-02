using System.Text.Json;
using Api.Middleware;
using Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace SeguimientoDeObjetivos.Tests;

// El 2026-09-01, con la BD de Azure recien despertando del auto-pause, la API
// contesto a un login con "400 Bad Request" y este Detail:
//
//   "An exception has been raised that is likely due to a transient failure.
//    Consider enabling transient error resiliency by adding
//    'EnableRetryOnFailure' to the 'UseSqlServer' call."
//
// Es el mensaje interno de EF Core, mostrado en pantalla al usuario. Pasaba
// porque el handler convertia en 400 CUALQUIER InvalidOperationException, y EF
// lanza una cuando pierde SQL Server. Un fallo de infraestructura se estaba
// presentando como un error del formulario.
public class GlobalExceptionHandlerTests
{
    private static async Task<(int status, string body)> Manejar(Exception ex)
    {
        var context = new DefaultHttpContext();
        var cuerpo = new MemoryStream();
        context.Response.Body = cuerpo;

        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var manejada = await handler.TryHandleAsync(context, ex, CancellationToken.None);

        Assert.True(manejada);
        cuerpo.Position = 0;
        return (context.Response.StatusCode, await new StreamReader(cuerpo).ReadToEndAsync());
    }

    private static string? Detail(string json) =>
        JsonDocument.Parse(json).RootElement.TryGetProperty("detail", out var d) ? d.GetString() : null;

    [Fact]
    public async Task Una_regla_de_negocio_da_400_y_explica_el_motivo()
    {
        var (status, body) = await Manejar(new BusinessRuleException("Este usuario no permite ser seguido."));

        Assert.Equal(StatusCodes.Status400BadRequest, status);
        Assert.Equal("Este usuario no permite ser seguido.", Detail(body));
    }

    [Fact]
    public async Task Algo_que_no_existe_da_404()
    {
        var (status, _) = await Manejar(new NotFoundException("Task", 42));
        Assert.Equal(StatusCodes.Status404NotFound, status);
    }

    // El caso concreto que se vio en produccion.
    [Fact]
    public async Task El_error_transitorio_de_EF_no_se_confunde_con_un_error_del_usuario()
    {
        var (status, _) = await Manejar(new InvalidOperationException(
            "An exception has been raised that is likely due to a transient failure."));

        Assert.Equal(StatusCodes.Status500InternalServerError, status);
    }

    [Fact]
    public async Task Un_fallo_tecnico_nunca_filtra_su_mensaje_al_cliente()
    {
        const string interno = "Login failed for user 'admin'. Connection string: Server=...";
        var (_, body) = await Manejar(new InvalidOperationException(interno));

        Assert.DoesNotContain("Connection string", body);
        Assert.DoesNotContain("admin", body);
        Assert.Equal("Ocurrio un error inesperado. Intenta de nuevo mas tarde.", Detail(body));
    }

    [Fact]
    public async Task Una_excepcion_cualquiera_tambien_da_500_generico()
    {
        var (status, body) = await Manejar(new Exception("boom interno"));

        Assert.Equal(StatusCodes.Status500InternalServerError, status);
        Assert.DoesNotContain("boom interno", body);
    }
}
