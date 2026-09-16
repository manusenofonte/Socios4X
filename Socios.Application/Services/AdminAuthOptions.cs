namespace Socios.Application.Services;

public class AdminAuthOptions
{
    // URL pública del frontend, para armar el link de "Olvidé mi contraseña"
    public string FrontendBaseUrl { get; set; } = string.Empty;

    public int MaxFailedLoginAttempts { get; set; } = 5;

    public TimeSpan LockoutDuration { get; set; } = TimeSpan.FromMinutes(15);

    public TimeSpan TemporaryPasswordLifetime { get; set; } = TimeSpan.FromHours(48);

    public TimeSpan PasswordResetLinkLifetime { get; set; } = TimeSpan.FromMinutes(30);
}
