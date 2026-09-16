using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Socios.Application.Interfaces;
using Socios.Domain.Entities;

namespace Socios.Application.Services;

// Login, cambio de contraseña y "Olvidé mi contraseña" del panel de administración
public class AdminAuthService
{
    // Un solo mensaje para email inexistente, contraseña incorrecta, cuenta inactiva o
    // bloqueada: así nadie puede averiguar qué emails tienen cuenta.
    public const string InvalidCredentialsMessage =
        "Email o contraseña incorrectos. Después de varios intentos fallidos la cuenta se bloquea por 15 minutos.";

    public const string InvalidResetLinkMessage =
        "El link no es válido o ya venció. Pedí uno nuevo desde \"Olvidé mi contraseña\".";

    private readonly IAdminUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IAdminTokenService _tokens;
    private readonly IEmailSender _email;
    private readonly AdminAuthOptions _options;
    private readonly TimeProvider _time;
    private readonly ILogger<AdminAuthService> _logger;

    // Hash de referencia para gastar el mismo tiempo cuando el email no existe
    private readonly Lazy<string> _dummyHash;

    public AdminAuthService(
        IAdminUserRepository users,
        IPasswordHasher hasher,
        IAdminTokenService tokens,
        IEmailSender email,
        IOptions<AdminAuthOptions> options,
        TimeProvider time,
        ILogger<AdminAuthService> logger)
    {
        _users = users;
        _hasher = hasher;
        _tokens = tokens;
        _email = email;
        _options = options.Value;
        _time = time;
        _logger = logger;
        _dummyHash = new Lazy<string>(() => _hasher.Hash("contraseña-de-referencia-1"));
    }

    public async Task<AdminAuthResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var user = string.IsNullOrWhiteSpace(email)
            ? null
            : await _users.GetByEmailAsync(AdminUser.NormalizeEmail(email), cancellationToken);

        if (user is null || user.PasswordHash is null)
        {
            _hasher.Verify(_dummyHash.Value, password ?? string.Empty);
            return AdminAuthResult.Fail(InvalidCredentialsMessage);
        }

        // Bloqueada o inactiva: ni siquiera se verifica la contraseña
        if (!user.IsActive || user.IsLockedOut(now))
            return AdminAuthResult.Fail(InvalidCredentialsMessage);

        if (!_hasher.Verify(user.PasswordHash, password ?? string.Empty))
        {
            user.RegisterFailedLogin(now, _options.MaxFailedLoginAttempts, _options.LockoutDuration);
            await _users.SaveChangesAsync(cancellationToken);
            _logger.LogWarning("Login fallido para el usuario admin {UserId}", user.Id);
            return AdminAuthResult.Fail(InvalidCredentialsMessage);
        }

        // La contraseña es correcta: acá sí se puede explicar el motivo
        if (user.IsTemporaryPasswordExpired(now))
            return AdminAuthResult.Fail("La contraseña temporal venció. Pedile al administrador principal que genere una nueva.");

        user.RegisterSuccessfulLogin(now);
        await _users.SaveChangesAsync(cancellationToken);

        return AdminAuthResult.Success(user, _tokens.CreateSession(user));
    }

    public async Task<AdminAuthResult> ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive || user.PasswordHash is null)
            return AdminAuthResult.Fail("Tu sesión ya no es válida. Volvé a ingresar.");

        if (!_hasher.Verify(user.PasswordHash, currentPassword ?? string.Empty))
            return AdminAuthResult.Fail("La contraseña actual es incorrecta.");

        var policyError = AdminPasswords.Validate(newPassword);
        if (policyError is not null)
            return AdminAuthResult.Fail(policyError);

        if (newPassword == currentPassword)
            return AdminAuthResult.Fail("La contraseña nueva tiene que ser distinta de la actual.");

        user.SetPassword(_hasher.Hash(newPassword));
        await _users.SaveChangesAsync(cancellationToken);

        // Las sesiones anteriores quedaron invalidadas: se entrega una nueva para seguir adentro
        return AdminAuthResult.Success(user, _tokens.CreateSession(user));
    }

    // No devuelve nada a propósito: la respuesta es la misma exista o no el email
    public async Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return;

        if (!_email.IsConfigured)
        {
            _logger.LogWarning("Se pidió restablecer una contraseña, pero no hay una cuenta de correo configurada (Email:Smtp)");
            return;
        }

        var user = await _users.GetByEmailAsync(AdminUser.NormalizeEmail(email), cancellationToken);
        if (user is null || !user.IsActive)
            return;

        var token = AdminPasswords.GenerateResetToken();
        var expiresAt = _time.GetUtcNow().UtcDateTime.Add(_options.PasswordResetLinkLifetime);
        user.StartPasswordReset(AdminPasswords.HashResetToken(token), expiresAt);
        await _users.SaveChangesAsync(cancellationToken);

        var link = $"{_options.FrontendBaseUrl.TrimEnd('/')}/admin/restablecer?token={Uri.EscapeDataString(token)}";
        var minutes = (int)_options.PasswordResetLinkLifetime.TotalMinutes;
        var body = $"""
            <p>Hola {WebUtility.HtmlEncode(user.Name)},</p>
            <p>Recibimos un pedido para restablecer tu contraseña del panel de administración del Asistente Virtual 4x.</p>
            <p><a href="{WebUtility.HtmlEncode(link)}">Elegir una contraseña nueva</a></p>
            <p>El link sirve una sola vez y vence en {minutes} minutos. Si no lo pediste, ignorá este correo: tu contraseña no cambia.</p>
            """;

        try
        {
            await _email.SendAsync(user.Email, "Restablecer tu contraseña - Asistente Virtual 4x", body, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo enviar el mail para restablecer la contraseña del usuario admin {UserId}", user.Id);
        }
    }

    public async Task<AdminAuthResult> ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return AdminAuthResult.Fail(InvalidResetLinkMessage);

        var user = await _users.GetByPasswordResetTokenHashAsync(AdminPasswords.HashResetToken(token), cancellationToken);
        var now = _time.GetUtcNow().UtcDateTime;

        if (user is null || !user.IsActive || user.PasswordResetTokenExpiresAt is null || user.PasswordResetTokenExpiresAt <= now)
            return AdminAuthResult.Fail(InvalidResetLinkMessage);

        var policyError = AdminPasswords.Validate(newPassword);
        if (policyError is not null)
            return AdminAuthResult.Fail(policyError);

        // SetPassword también invalida el link y todas las sesiones abiertas
        user.SetPassword(_hasher.Hash(newPassword));
        await _users.SaveChangesAsync(cancellationToken);

        return AdminAuthResult.Success(user, session: null);
    }
}

public record AdminAuthResult(bool Succeeded, string? Error, AdminUser? User, AdminSession? Session)
{
    public static AdminAuthResult Success(AdminUser user, AdminSession? session) => new(true, null, user, session);

    public static AdminAuthResult Fail(string error) => new(false, error, null, null);
}
