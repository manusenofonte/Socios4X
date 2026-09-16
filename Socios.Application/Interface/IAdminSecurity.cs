using Socios.Domain.Entities;

namespace Socios.Application.Interfaces;

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string passwordHash, string password);
}

public interface IEmailSender
{
    // false mientras no haya una cuenta de correo configurada
    bool IsConfigured { get; }

    Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);
}

public interface IAdminTokenService
{
    AdminSession CreateSession(AdminUser user);
}

public record AdminSession(string Token, DateTime ExpiresAt);
