using Microsoft.AspNetCore.Identity;
using Socios.Domain.Entities;
using IAppPasswordHasher = Socios.Application.Interfaces.IPasswordHasher;

namespace Socios.Infrastructure.Security;

// Usa el hasher de ASP.NET Core Identity: PBKDF2 con SHA-512, salt aleatorio y 100.000 iteraciones
public class IdentityPasswordHasher : IAppPasswordHasher
{
    private static readonly PasswordHasher<AdminUser> Hasher = new();

    public string Hash(string password) => Hasher.HashPassword(null!, password);

    public bool Verify(string passwordHash, string password)
    {
        try
        {
            return Hasher.VerifyHashedPassword(null!, passwordHash, password) != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            // Hash con formato inválido (dato corrupto): nunca debe dejar pasar
            return false;
        }
    }
}
