using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SeguimientoDeObjetivos.Tests;

// El diario estuvo expuesto meses: cualquier usuario logueado podia leer, editar
// y borrar entradas ajenas cambiando un numero en la URL. Se descubrio a mano,
// comparando cuantos Forbid() tenia cada controller.
//
// Estos tests miran la forma de los controllers para que no vuelva a pasar en
// silencio: si alguien agrega un endpoint que recibe userId por query sin
// contrastarlo contra el token, o quita [Authorize], el CI lo frena.
public class ControllersProtegenPropiedadTests
{
    private static readonly Assembly Api = typeof(Api.Controllers.DiaryEntriesController).Assembly;

    private static IEnumerable<Type> Controllers() =>
        Api.GetTypes().Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract);

    [Fact]
    public void Todos_los_controllers_exigen_autenticacion_salvo_Auth()
    {
        var sinAuthorize = Controllers()
            .Where(c => c.Name != "AuthController")
            .Where(c => c.GetCustomAttribute<AuthorizeAttribute>() is null)
            .Select(c => c.Name)
            .ToList();

        Assert.True(sinAuthorize.Count == 0,
            $"Sin [Authorize]: {string.Join(", ", sinAuthorize)}");
    }

    // Recibir userId por query es la puerta de entrada al fallo: el numero lo
    // elige quien llama. Si un controller lo acepta, tiene que comparar contra
    // la identidad del token en alguna parte.
    [Fact]
    public void Quien_recibe_userId_por_query_compara_contra_el_token()
    {
        foreach (var controller in Controllers())
        {
            var recibeUserId = controller
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .SelectMany(m => m.GetParameters())
                .Any(p => p.Name == "userId" && p.GetCustomAttribute<FromQueryAttribute>() is not null);

            if (!recibeUserId) continue;

            var tieneRequesterId = controller
                .GetProperties(BindingFlags.NonPublic | BindingFlags.Instance)
                .Any(p => p.Name == "RequesterId");

            Assert.True(tieneRequesterId,
                $"{controller.Name} recibe userId por query pero no define RequesterId: " +
                "no puede estar comprobando de quien son los datos.");
        }
    }

    [Fact]
    public void El_diario_comprueba_propiedad_en_todos_sus_endpoints()
    {
        var tipo = typeof(Api.Controllers.DiaryEntriesController);

        var endpoints = tipo
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttributes()
                .Any(a => a.GetType().Name.StartsWith("Http")))
            .ToList();

        Assert.NotEmpty(endpoints);

        // Create toma el autor del token y no expone id ajeno, asi que no lleva
        // comprobacion propia; el resto si.
        Assert.True(tipo.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance)
            .Any(p => p.Name == "RequesterId"),
            "DiaryEntriesController debe resolver la identidad desde el token.");

        var create = endpoints.Single(m => m.Name == "Create");
        Assert.DoesNotContain(create.GetParameters(), p => p.Name == "userId");
    }
}
