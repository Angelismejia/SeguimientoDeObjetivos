using Api;
using Api.Hubs;
using Api.Middleware;
using Application;
using Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
var key = Encoding.ASCII.GetBytes(jwtSecret);

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

// Limite de intentos en login/registro: sin esto, nada impedia probar
// contraseñas o emails en loop contra /api/auth. 5 intentos por minuto por IP
// alcanza de sobra para un usuario real que se equivoca de contraseña, y
// frena fuerza bruta/credential stuffing.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("auth", limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
    });
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
app.MapFallbackToFile("index.html", noCacheStaticFileOptions);

app.Run();
