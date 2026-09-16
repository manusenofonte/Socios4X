using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Socios.Application.Interfaces;
using Socios.Domain.Entities;

namespace Socios.Api.Auth;

public static class AdminClaims
{
    public const string UserId = "sub";
    public const string Email = "email";
    public const string Name = "name";
    public const string Role = "role";
    public const string SessionVersion = "sv";
    public const string MustChangePassword = "pwd_change";
}

public static class AdminPolicies
{
    // Usuario logueado que ya eligió su contraseña (no tiene una temporal)
    public const string Panel = "AdminPanel";

    // Igual que Panel, y además es el administrador principal
    public const string SuperAdmin = "SuperAdmin";
}

public record AdminJwtSettings(SymmetricSecurityKey SigningKey, string Issuer, string Audience, TimeSpan Lifetime);

public class JwtAdminTokenService : IAdminTokenService
{
    private readonly AdminJwtSettings _settings;
    private readonly TimeProvider _time;

    public JwtAdminTokenService(AdminJwtSettings settings, TimeProvider time)
    {
        _settings = settings;
        _time = time;
    }

    public AdminSession CreateSession(AdminUser user)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var expiresAt = now.Add(_settings.Lifetime);

        var claims = new List<Claim>
        {
            new(AdminClaims.UserId, user.Id.ToString()),
            new(AdminClaims.Email, user.Email),
            new(AdminClaims.Name, user.Name),
            new(AdminClaims.Role, user.Role),
            new(AdminClaims.SessionVersion, user.SessionVersion.ToString())
        };

        if (user.MustChangePassword)
            claims.Add(new Claim(AdminClaims.MustChangePassword, "true"));

        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(_settings.SigningKey, SecurityAlgorithms.HmacSha256)
        });

        return new AdminSession(token, expiresAt);
    }
}

public static class ClaimsPrincipalExtensions
{
    public static int GetAdminUserId(this ClaimsPrincipal principal) =>
        int.Parse(principal.FindFirstValue(AdminClaims.UserId)!);
}
