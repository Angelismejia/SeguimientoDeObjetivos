using Api;
using Api.Hubs;
using Api.Middleware;
using Application;
using Application.Common;
using Application.Interfaces.Repositories;
using Infrastructure;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddAzureWebAppDiagnostics();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Sin fallback hardcodeado a proposito: si falta la config, la app debe fallar
// al arrancar en vez de firmar tokens con un secreto conocido/predecible.
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret no esta configurado. Local: 'dotnet user-secrets set Jwt:Secret <valor>'. Azure: Application setting 'Jwt__Secret'.");
// UTF8, igual que AuthService al firmar: con ASCII, un secreto con caracteres
// no ASCII se convertia en "?" de un lado y no del otro, y ningun token validaba.
var key = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "SeguimientoObjetivos",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "SeguimientoObjetivosUsers",
            ValidateLifetime = true
        };

        // SignalR no puede mandar el header Authorization en la conexión WebSocket,
        // así que el token viaja como query string (?access_token=...) y lo leemos acá.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            },

            // La firma y la fecha ya las valido el middleware; esto agrega que la
            // sesion siga siendo legitima. Se rechaza el token si la cuenta ya no
            // existe, si se desactivo, o si la contraseña cambio despues de emitirlo
            // (el sello no coincide). Tambien caen los tokens viejos que no traen
            // sello: eso obliga a todos a iniciar sesion una vez tras este deploy.
            //
            // Cuesta una consulta por clave primaria por peticion. En SignalR se
            // valida al conectar, asi que un chat ya abierto sigue vivo hasta que
            // se reconecte.
            OnTokenValidated = async context =>
            {
                var id = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var sello = context.Principal?.FindFirst(SelloDeSesion.Claim)?.Value;
                if (!int.TryParse(id, out var userId) || sello is null)
                {
                    context.Fail("Token sin sello de sesion.");
                    return;
                }

                var users = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
                var user = await users.GetByIdAsync(userId);
                if (!SelloDeSesion.SigueVigente(user, sello))
                    context.Fail("La sesion ya no es valida.");
            }
        };
    });

// El frontend se sirve desde este mismo origen (Angular compilado a wwwroot,
// ver el workflow de deploy), asi que en produccion no deberia ni activarse
// esta politica para el trafico normal de la app. La dejamos como allowlist
// explicita igual, en vez de AllowAnyOrigin, para que un sitio de terceros
// no pueda invocar la API usando un token robado por otro medio (XSS, etc.).
// En Development se deja abierto porque ahi se prueba con Swagger, un
// devtunnel u otro puerto local, y no hay datos reales en juego.
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AppCors", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        }
        else
        {
            policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader();
        }
    });
});

// Limite de intentos en /api/auth (login, registro, recuperar contraseña): sin
// esto, nada impedia probar contraseñas o emails en loop. 5 por minuto POR IP
// alcanza de sobra para un usuario real que se equivoca de contraseña, y frena
// fuerza bruta/credential stuffing.
//
// Antes era AddFixedWindowLimiter, que pese al comentario NO separaba por IP:
// era un unico contador compartido por todo el mundo. Cinco personas
// iniciando sesion en el mismo minuto le dejaban el login en 429 a la sexta, y
// un atacante podia bloquearle el acceso a todos con 5 peticiones por minuto.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// En Azure la app esta detras de un proxy: sin esto, RemoteIpAddress seria la
// IP del proxy y el limite por IP de arriba volveria a ser un contador global.
// ForwardLimit = 1 lee solo la ULTIMA entrada de X-Forwarded-For, la que agrega
// el proxy de Azure; las anteriores las puede inventar el cliente. Se vacian
// KnownNetworks/KnownProxies porque la IP del proxy de Azure no es fija.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Primero de todo, para que todo lo que sigue (rate limiter, HTTPS, logs) vea
// la IP y el esquema reales del cliente.
app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();

// Headers de seguridad basicos que ASP.NET Core no manda por defecto.
// CSP: 'self' para todo salvo lo que la app realmente usa (fuente Inter de
// Google Fonts, estilos inline que inyecta Angular Material/CDK en overlays).
// OnStarting (no seteo directo antes de next()) porque UseExceptionHandler
// hace Response.Clear() cuando atrapa una excepcion, y eso borraba headers
// puestos antes; OnStarting corre justo antes de mandar la respuesta, ya
// despues de cualquier Clear(), asi que sobrevive tanto en éxito como error.
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Content-Security-Policy"] =
            "default-src 'self'; " +
            "script-src 'self'; " +
            "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
            "font-src 'self' https://fonts.gstatic.com; " +
            "img-src 'self' data:; " +
            "connect-src 'self'; " +
            "frame-ancestors 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self';";
        return Task.CompletedTask;
    });
    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors("AppCors");
app.UseRateLimiter();

// index.html (y el manifest) no deben quedar cacheados por el navegador: es el
// "shell" que referencia los archivos con hash de cada build. Si el navegador
// lo guarda en caché sin revalidar, un deploy nuevo puede tardar horas en
// llegarle a un usuario aunque el servidor ya tenga la version nueva. Los
// archivos con hash (chunk-*.js, styles-*.css) si pueden cachearse largo,
// porque cada build genera nombres distintos.
var noCacheStaticFileOptions = new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        var fileName = ctx.File.Name;
        if (fileName.Equals("index.html", StringComparison.OrdinalIgnoreCase) ||
            fileName.Equals("manifest.webmanifest", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Context.Response.Headers.CacheControl = "no-cache, must-revalidate";
        }
    }
};
app.UseStaticFiles(noCacheStaticFileOptions);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(PersistentStorage.UploadsPath(app.Environment)),
    RequestPath = "/uploads"
});
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

// Lo llama cada 5 minutos el workflow .github/workflows/mantener-despierta.yml.
// En el plan gratuito, Azure apaga la app tras unos 20 minutos sin trafico, y con
// ella se para TaskReminderBackgroundService: los recordatorios de "es hora de tu
// tarea" solo salian si justo alguien estaba usando la app. Toca la base a
// proposito, asi Supabase tampoco se pausa por inactividad.
app.MapGet("/api/health", async (ApplicationDbContext db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct)
        ? Results.Ok(new { status = "ok" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable))
    .AllowAnonymous();
app.MapFallbackToFile("index.html", noCacheStaticFileOptions);

app.Run();
