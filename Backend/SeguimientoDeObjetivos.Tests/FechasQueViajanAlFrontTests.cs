using System.Text.Json;
using Domain.Entities;

namespace SeguimientoDeObjetivos.Tests;

// Todas las marcas de tiempo de las entidades nacen como DateTime.UtcNow, pero
// las columnas son "timestamp without time zone": Npgsql las devuelve con
// DateTimeKind.Unspecified, y System.Text.Json serializa eso SIN la Z final.
//
// El navegador interpreta una fecha sin zona como hora local. Como el valor es
// UTC, cada hora que muestra la app queda corrida por el desfase de la zona
// (4 horas aqui). Estos tests fijan ese contrato para que quede escrito: el
// frontend tiene que leer estas fechas como UTC (ver fechaDelBackend en
// core/utils/fecha.util.ts).
public class FechasQueViajanAlFrontTests
{
    [Fact]
    public void Una_fecha_sin_zona_se_serializa_sin_la_Z()
    {
        var comoLaDevuelveNpgsql = new DateTime(2026, 9, 30, 20, 40, 5, DateTimeKind.Unspecified);

        var json = JsonSerializer.Serialize(comoLaDevuelveNpgsql);

        Assert.DoesNotContain("Z", json);
        Assert.Contains("2026-09-30T20:40:05", json);
    }

    [Fact]
    public void Una_fecha_marcada_como_UTC_si_lleva_la_Z()
    {
        var json = JsonSerializer.Serialize(
            new DateTime(2026, 9, 30, 20, 40, 5, DateTimeKind.Utc));

        Assert.Contains("Z", json);
    }

    // Si algun dia alguien cambia esto a la hora local del servidor, el frontend
    // empezaria a restar horas de mas sin que nadie se entere.
    [Fact]
    public void Las_marcas_de_tiempo_de_las_entidades_nacen_en_UTC()
    {
        var antes = DateTime.UtcNow.AddSeconds(-5);

        var mensaje = new Message();
        var notificacion = new Notification();

        Assert.InRange(mensaje.SentAt, antes, DateTime.UtcNow.AddSeconds(5));
        Assert.InRange(notificacion.CreatedAt, antes, DateTime.UtcNow.AddSeconds(5));
    }
}
