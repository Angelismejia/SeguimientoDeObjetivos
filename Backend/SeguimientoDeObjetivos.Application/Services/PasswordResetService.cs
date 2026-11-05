using System.Net;
using Application.Common;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Exceptions;
using Microsoft.Extensions.Configuration;

namespace Application.Services
{
    public class PasswordResetService : IPasswordResetService
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _emailSender;
        private readonly string _secreto;
        private readonly string _urlPublica;
        private readonly Func<DateTime> _ahoraUtc;

        public PasswordResetService(
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            IEmailSender emailSender,
            IConfiguration configuration)
            : this(userRepository, unitOfWork, emailSender, configuration, () => DateTime.UtcNow) { }

        // Los tests pasan el reloj para poder probar el vencimiento.
        public PasswordResetService(
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            IEmailSender emailSender,
            IConfiguration configuration,
            Func<DateTime> ahoraUtc)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
            _secreto = configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
            // A donde apunta el enlace del correo: la URL de la app (en Azure, la
            // misma que sirve la API) o el ng serve local.
            _urlPublica = (configuration["App:PublicUrl"]
                ?? configuration["AllowedOrigins:0"]
                ?? "http://localhost:4200").TrimEnd('/');
            _ahoraUtc = ahoraUtc;
        }

        // Nunca dice si el correo existe: responder distinto permitiria averiguar
        // quien tiene cuenta probando direcciones. Por eso un correo desconocido
        // (o de una cuenta desactivada) termina igual que uno valido, sin error.
        public async Task RequestAsync(string email, CancellationToken ct = default)
        {
            var user = await _userRepository.GetByEmailAsync(email.Trim());
            if (user is null || !user.IsActive) return;

            var token = EnlaceDeRecuperacion.Crear(user.Id, user.PasswordHash,
                _ahoraUtc().Add(EnlaceDeRecuperacion.Vigencia), _secreto);
            var enlace = $"{_urlPublica}/reset-password?token={Uri.EscapeDataString(token)}";
            var nombre = WebUtility.HtmlEncode(user.Name);

            await _emailSender.SendAsync(user.Email, "Restablece tu contraseña de BetterMe", $"""
                <div style="font-family:Arial,sans-serif;max-width:480px;margin:auto;color:#1f2937">
                  <h2 style="color:#4f46e5">Hola, {nombre}</h2>
                  <p>Alguien pidió restablecer la contraseña de tu cuenta de BetterMe. Si fuiste tú, usa este botón:</p>
                  <p style="text-align:center;margin:28px 0">
                    <a href="{enlace}" style="background:#4f46e5;color:#fff;padding:12px 22px;border-radius:10px;text-decoration:none;font-weight:bold">Elegir una contraseña nueva</a>
                  </p>
                  <p style="font-size:13px;color:#6b7280">El enlace vale por 1 hora y sirve una sola vez. Si no lo pediste, ignora este correo: tu contraseña sigue igual.</p>
                </div>
                """, ct);
        }

        public async Task ResetAsync(string token, string newPassword)
        {
            const string invalido = "El enlace no es válido o ya venció. Pide uno nuevo.";

            var datos = EnlaceDeRecuperacion.Leer(token, _secreto, _ahoraUtc());
            if (datos is null) throw new BusinessRuleException(invalido);

            var user = await _userRepository.GetByIdAsync(datos.Value.UserId);
            if (user is null || !user.IsActive
                || EnlaceDeRecuperacion.Huella(user.PasswordHash) != datos.Value.Huella)
                throw new BusinessRuleException(invalido);

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
