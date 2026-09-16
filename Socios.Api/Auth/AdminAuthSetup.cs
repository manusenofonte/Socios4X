using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Socios.Application.Interfaces;
using Socios.Application.Services;
using Socios.Domain.Entities;

namespace Socios.Api.Auth;

public static class AdminAuthSetup
{
    private const string Issuer = "socios-api";
    private const string Audience = "socios-admin";

    public static IServiceCollection AddAdminAuth(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var jwtKey = configuration["Auth:JwtKey"];
        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            if (!environment.IsDevelopment())
                throw new InvalidOperationException("Falta configurar Auth:JwtKey (una clave aleatoria de al menos 32 caracteres).");

            // En desarrollo se genera una por arranque: las sesiones se pierden al reiniciar la API
            jwtKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        }

        if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
            throw new InvalidOperationException("Auth:JwtKey tiene que tener al menos 32 caracteres.");

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        services.AddSingleton(new AdminJwtSettings(signingKey, Issuer, Audience, TimeSpan.FromHours(8)));
        services.AddSingleton<IAdminTokenService, JwtAdminTokenService>();

        services.Configure<AdminAuthOptions>(options =>
            options.FrontendBaseUrl = configuration["Auth:FrontendBaseUrl"] ?? string.Empty);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = Issuer,
                    ValidateAudience = true,
                    ValidAudience = Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = AdminClaims.Email,
                    RoleClaimType = AdminClaims.Role
                };

                // Un token firmado no alcanza: el usuario tiene que seguir activo y la sesión no
                // puede haber sido invalidada (cambio o blanqueo de contraseña, desactivación).
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var principal = context.Principal!;
                        if (!int.TryParse(principal.FindFirst(AdminClaims.UserId)?.Value, out var userId)
                            || !int.TryParse(principal.FindFirst(AdminClaims.SessionVersion)?.Value, out var sessionVersion))
                        {
                            context.Fail("Token incompleto.");
                            return;
                        }

                        var users = context.HttpContext.RequestServices.GetRequiredService<IAdminUserRepository>();
                        var user = await users.GetByIdAsync(userId, context.HttpContext.RequestAborted);

                        if (user is null || !user.IsActive || user.SessionVersion != sessionVersion)
                            context.Fail("La sesión ya no es válida.");
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AdminPolicies.Panel, policy => policy
                .RequireAuthenticatedUser()
                .RequireAssertion(context => !context.User.HasClaim(AdminClaims.MustChangePassword, "true")));

            options.AddPolicy(AdminPolicies.SuperAdmin, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(AdminUser.RoleSuperAdmin)
                .RequireAssertion(context => !context.User.HasClaim(AdminClaims.MustChangePassword, "true")));
        });

        return services;
    }
}
