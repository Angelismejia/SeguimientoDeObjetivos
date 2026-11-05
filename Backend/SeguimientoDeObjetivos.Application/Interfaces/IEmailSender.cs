namespace Application.Interfaces
{
    /// <summary>
    /// Manda un correo. La implementacion (SMTP con MailKit) vive en
    /// Infrastructure; Application solo sabe que puede mandar uno.
    /// </summary>
    public interface IEmailSender
    {
        Task SendAsync(string to, string subject, string html, CancellationToken ct = default);
    }
}
