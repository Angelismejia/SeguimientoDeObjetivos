using Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Infrastructure.Email
{
    /// <summary>
    /// Manda correos por SMTP. Sirve igual para Brevo, Gmail o cualquier otro
    /// proveedor: solo cambia la configuracion de la seccion "Email".
    ///
    /// <para>
    /// Usuario y clave NO van en appsettings.json. En local:
    /// <c>dotnet user-secrets set Email:Password &lt;valor&gt;</c>. En Azure:
    /// Application settings con doble guion bajo (<c>Email__Password</c>).
    /// </para>
    ///
    /// <para>
    /// Si no hay servidor configurado no falla: anota en el log que no mando nada.
    /// En Development ademas escribe el contenido, asi se puede probar "olvide mi
    /// contraseña" en local copiando el enlace del log.
    /// </para>
    /// </summary>
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _environment;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IConfiguration configuration, IHostEnvironment environment, ILogger<SmtpEmailSender> logger)
        {
            _configuration = configuration;
            _environment = environment;
            _logger = logger;
        }

        public async Task SendAsync(string to, string subject, string html, CancellationToken ct = default)
        {
            var host = _configuration["Email:Host"];
            if (string.IsNullOrWhiteSpace(host))
            {
                _logger.LogWarning("Email:Host no esta configurado; no se mando el correo \"{Subject}\".", subject);
                if (_environment.IsDevelopment())
                    _logger.LogInformation("Correo para {To}:\n{Html}", to, html);
                return;
            }

            // Con servidor pero sin credenciales es un error de configuracion, no un
            // caso normal: se dice claro que falta en vez de fallar adentro de MailKit.
            var usuario = _configuration["Email:User"];
            var clave = _configuration["Email:Password"];
            var remitente = _configuration["Email:FromAddress"] is { Length: > 0 } desde ? desde : usuario;
            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(clave) || string.IsNullOrWhiteSpace(remitente))
                throw new InvalidOperationException("Email:Host esta configurado pero faltan Email:User, Email:Password o Email:FromAddress.");

            var mensaje = new MimeMessage();
            mensaje.From.Add(new MailboxAddress(_configuration["Email:FromName"] ?? "BetterMe", remitente));
            mensaje.To.Add(MailboxAddress.Parse(to));
            mensaje.Subject = subject;
            mensaje.Body = new BodyBuilder { HtmlBody = html }.ToMessageBody();

            using var smtp = new SmtpClient { Timeout = 20000 };
            var puerto = int.TryParse(_configuration["Email:Port"], out var p) ? p : 587;
            // 465 es SSL directo; 587 (lo normal en Brevo y Gmail) arranca en claro y sube a TLS.
            await smtp.ConnectAsync(host, puerto,
                puerto == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls, ct);
            await smtp.AuthenticateAsync(usuario, clave, ct);
            await smtp.SendAsync(mensaje, ct);
            await smtp.DisconnectAsync(true, ct);
        }
    }
}
