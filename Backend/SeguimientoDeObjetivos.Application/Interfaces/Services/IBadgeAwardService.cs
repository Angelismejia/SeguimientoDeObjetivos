namespace Application.Interfaces.Services
{
    public interface IBadgeAwardService
    {
        // Revisa el progreso real del usuario (tareas, objetivos, racha) y le
        // otorga cualquier insignia que ya haya ganado y todavía no tenga.
        Task CheckAndAwardAsync(int userId);
    }
}
