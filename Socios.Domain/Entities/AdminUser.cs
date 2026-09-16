namespace Socios.Domain.Entities;

// Usuario del panel de administración del asistente. Es independiente de la tabla Users
// del Sistema de Socios Web, para no arriesgar el login del sistema principal.
public class AdminUser
{
    public const string RoleSuperAdmin = "SuperAdmin";
    public const string RoleAdmin = "Admin";

    public int Id { get; private set; }

    public string Email { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Role { get; private set; } = RoleAdmin;

    // Hash de la contraseña (nunca la contraseña en texto plano)
    public string? PasswordHash { get; private set; }

    // true mientras tenga una contraseña temporal: tiene que elegir la suya para usar el panel
    public bool MustChangePassword { get; private set; }

    public DateTime? TemporaryPasswordExpiresAt { get; private set; }

    public bool IsActive { get; private set; } = true;

    public int FailedLoginAttempts { get; private set; }

    public DateTime? LockoutEndsAt { get; private set; }

    // Va en el token de sesión: al cambiarlo, todas las sesiones abiertas dejan de valer
    public int SessionVersion { get; private set; }

    // Hash del token de "Olvidé mi contraseña" (el token en sí solo viaja en el mail)
    public string? PasswordResetTokenHash { get; private set; }

    public DateTime? PasswordResetTokenExpiresAt { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? LastLoginAt { get; private set; }

    // Constructor privado para EF Core
    private AdminUser() { }

    public static AdminUser Create(string email, string name, string role)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty");

        if (role != RoleSuperAdmin && role != RoleAdmin)
            throw new ArgumentException($"Invalid role: {role}");

        return new AdminUser
        {
            Email = NormalizeEmail(email),
            Name = string.IsNullOrWhiteSpace(name) ? NormalizeEmail(email) : name.Trim(),
            Role = role,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public bool IsSuperAdmin => Role == RoleSuperAdmin;

    public bool IsLockedOut(DateTime now) => LockoutEndsAt.HasValue && LockoutEndsAt.Value > now;

    public bool IsTemporaryPasswordExpired(DateTime now) =>
        MustChangePassword && TemporaryPasswordExpiresAt.HasValue && TemporaryPasswordExpiresAt.Value <= now;

    public void SetTemporaryPassword(string passwordHash, DateTime expiresAt)
    {
        PasswordHash = passwordHash;
        MustChangePassword = true;
        TemporaryPasswordExpiresAt = expiresAt;
        ClearPasswordReset();
        ClearLockout();
        SessionVersion++;
    }

    public void SetPassword(string passwordHash)
    {
        PasswordHash = passwordHash;
        MustChangePassword = false;
        TemporaryPasswordExpiresAt = null;
        ClearPasswordReset();
        ClearLockout();
        SessionVersion++;
    }

    public void RegisterFailedLogin(DateTime now, int maxAttempts, TimeSpan lockoutDuration)
    {
        FailedLoginAttempts++;

        if (FailedLoginAttempts >= maxAttempts)
        {
            LockoutEndsAt = now.Add(lockoutDuration);
            FailedLoginAttempts = 0;
        }
    }

    public void RegisterSuccessfulLogin(DateTime now)
    {
        ClearLockout();
        LastLoginAt = now;
    }

    public void StartPasswordReset(string tokenHash, DateTime expiresAt)
    {
        PasswordResetTokenHash = tokenHash;
        PasswordResetTokenExpiresAt = expiresAt;
    }

    public void Deactivate()
    {
        IsActive = false;
        ClearPasswordReset();
        SessionVersion++;
    }

    public void Activate()
    {
        IsActive = true;
    }

    private void ClearPasswordReset()
    {
        PasswordResetTokenHash = null;
        PasswordResetTokenExpiresAt = null;
    }

    private void ClearLockout()
    {
        FailedLoginAttempts = 0;
        LockoutEndsAt = null;
    }
}
