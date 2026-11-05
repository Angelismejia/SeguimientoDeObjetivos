namespace Application.Interfaces.Services
{
    /// <summary>"Olvide mi contraseña": pedir el enlace por correo y usarlo.</summary>
    public interface IPasswordResetService
    {
        Task RequestAsync(string email, CancellationToken ct = default);
        Task ResetAsync(string token, string newPassword);
    }
}
