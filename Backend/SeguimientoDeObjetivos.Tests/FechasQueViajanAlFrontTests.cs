using System.Text.Json;
using Application.DTOs.Messages;
using Application.DTOs.Tasks;
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

    // Lo de arriba es el problema. Esto es el arreglo: los instantes de los DTOs
    // llevan [JsonConverter(typeof(InstanteUtcJsonConverter))], que los marca como
    // UTC al serializar para que el JSON salga con la Z.
    [Fact]
    public void Un_instante_de_un_DTO_sale_con_la_Z()
    {
        var json = JsonSerializer.Serialize(new MessageDto
        {
            SentAt = new DateTime(2026, 9, 30, 20, 40, 5, DateTimeKind.Unspecified)
        });

        Assert.Contains("2026-09-30T20:40:05Z", json);
    }

    [Fact]
    public void Un_instante_nulo_sigue_saliendo_nulo()
    {
        var json = JsonSerializer.Serialize(new MessageDto { ReadAt = null });

        Assert.Contains("\"ReadAt\":null", json);
    }

    // El guard que importa. Una fecha suelta representa un DIA, no un instante.
    // Si le pusieramos la Z, un "2026-09-30T00:00:00" se mostraria como el 29 de
    // septiembre a las 20:00 en una zona UTC-4: correria las tareas y el diario un
    // dia entero. Por eso el conversor va propiedad por propiedad y no global.
    [Fact]
    public void Una_fecha_suelta_NO_lleva_la_Z_aunque_este_al_lado_de_un_instante()
    {
        var json = JsonSerializer.Serialize(new TaskDto
        {
            ScheduledDate = new DateTime(2026, 9, 30, 0, 0, 0, DateTimeKind.Unspecified),
            EndRepeatDate = new DateTime(2026, 10, 31, 0, 0, 0, DateTimeKind.Unspecified),
            CreatedAt = new DateTime(2026, 9, 30, 20, 40, 5, DateTimeKind.Unspecified)
        });

        Assert.Contains("\"ScheduledDate\":\"2026-09-30T00:00:00\"", json);
        Assert.Contains("\"EndRepeatDate\":\"2026-10-31T00:00:00\"", json);

        // y el instante que viaja en el mismo objeto si la lleva
        Assert.Contains("\"CreatedAt\":\"2026-09-30T20:40:05Z\"", json);
    }

    [Fact]
    public void Las_fechas_de_CompletedDates_tampoco_llevan_Z()
    {
        var json = JsonSerializer.Serialize(new TaskDto
        {
            CompletedDates = { new DateTime(2026, 9, 29, 0, 0, 0, DateTimeKind.Unspecified) }
        });

        Assert.Contains("2026-09-29T00:00:00", json);
        Assert.DoesNotContain("2026-09-29T00:00:00Z", json);
    }
}
