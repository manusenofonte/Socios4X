using System.Net.Mail;
using Microsoft.Extensions.Options;
using Socios.Application.Interfaces;
using Socios.Domain.Entities;

namespace Socios.Application.Services;

// Alta, blanqueo de contraseña y activación de usuarios del panel (lo usa el superadmin)
public class AdminUserManagementService
{
    private readonly IAdminUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly AdminAuthOptions _options;
    private readonly TimeProvider _time;

    public AdminUserManagementService(
        IAdminUserRepository users,
        IPasswordHasher hasher,
        IOptions<AdminAuthOptions> options,
        TimeProvider time)
    {
        _users = users;
        _hasher = hasher;
        _options = options.Value;
        _time = time;
    }

    public Task<IReadOnlyList<AdminUser>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _users.GetAllAsync(cancellationToken);

    public async Task<AdminUserResult> CreateAdminAsync(string email, string name, CancellationToken cancellationToken = default)
    {
        if (!IsValidEmail(email))
            return AdminUserResult.Fail("El email no es válido.");

        if (await _users.GetByEmailAsync(AdminUser.NormalizeEmail(email), cancellationToken) is not null)
            return AdminUserResult.Fail("Ya existe un usuario con ese email.");

        var user = AdminUser.Create(email, name, AdminUser.RoleAdmin);
        var temporaryPassword = AssignTemporaryPassword(user);

        await _users.AddAsync(user, cancellationToken);
        return AdminUserResult.Success(user, temporaryPassword);
    }

    public async Task<AdminUserResult> ResetPasswordAsync(int actorId, int userId, CancellationToken cancellationToken = default)
    {
        if (actorId == userId)
            return AdminUserResult.Fail("Para cambiar tu propia contraseña usá \"Cambiar contraseña\".");

        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return AdminUserResult.NotFound();

        var temporaryPassword = AssignTemporaryPassword(user);
        await _users.SaveChangesAsync(cancellationToken);

        return AdminUserResult.Success(user, temporaryPassword);
    }

    public async Task<AdminUserResult> SetActiveAsync(int actorId, int userId, bool active, CancellationToken cancellationToken = default)
    {
        if (actorId == userId)
            return AdminUserResult.Fail("No podés desactivar tu propio usuario.");

        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return AdminUserResult.NotFound();

        if (user.IsSuperAdmin && !active)
            return AdminUserResult.Fail("No se puede desactivar al administrador principal.");

        if (active)
            user.Activate();
        else
            user.Deactivate();

        await _users.SaveChangesAsync(cancellationToken);
        return AdminUserResult.Success(user, temporaryPassword: null);
    }

    // Para el comando de consola: crea el superadmin o, si ya existe, le genera una
    // contraseña temporal nueva (sirve si el superadmin se olvida la suya).
    public async Task<AdminUserResult> CreateOrResetSuperAdminAsync(string email, string name, CancellationToken cancellationToken = default)
    {
        if (!IsValidEmail(email))
            return AdminUserResult.Fail("El email no es válido.");

        var user = await _users.GetByEmailAsync(AdminUser.NormalizeEmail(email), cancellationToken);

        if (user is not null && !user.IsSuperAdmin)
            return AdminUserResult.Fail("Ese email ya pertenece a un administrador que no es el principal.");

        if (user is null)
        {
            user = AdminUser.Create(email, name, AdminUser.RoleSuperAdmin);
            var password = AssignTemporaryPassword(user);
            await _users.AddAsync(user, cancellationToken);
            return AdminUserResult.Success(user, password);
        }

        user.Activate();
        var newPassword = AssignTemporaryPassword(user);
        await _users.SaveChangesAsync(cancellationToken);
        return AdminUserResult.Success(user, newPassword);
    }

    private string AssignTemporaryPassword(AdminUser user)
    {
        var temporaryPassword = AdminPasswords.GenerateTemporary();
        var expiresAt = _time.GetUtcNow().UtcDateTime.Add(_options.TemporaryPasswordLifetime);
        user.SetTemporaryPassword(_hasher.Hash(temporaryPassword), expiresAt);
        return temporaryPassword;
    }

    private static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > 256)
            return false;

        return MailAddress.TryCreate(email.Trim(), out var address) && address.Address == email.Trim();
    }
}

public record AdminUserResult(bool Succeeded, bool IsNotFound, string? Error, AdminUser? User, string? TemporaryPassword)
{
    public static AdminUserResult Success(AdminUser user, string? temporaryPassword) => new(true, false, null, user, temporaryPassword);

    public static AdminUserResult Fail(string error) => new(false, false, error, null, null);

    public static AdminUserResult NotFound() => new(false, true, "No se encontró el usuario.", null, null);
}
