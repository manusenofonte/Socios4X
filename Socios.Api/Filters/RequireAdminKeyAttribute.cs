using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Socios.Api.Filters;

// Exige el header X-Admin-Key con la clave configurada en Admin:UploadKey.
// Protege todos los endpoints del panel de administración.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireAdminKeyAttribute : Attribute, IAuthorizationFilter
{
    private const string AdminKeyHeader = "X-Admin-Key";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var adminKey = configuration["Admin:UploadKey"];
        var providedKey = context.HttpContext.Request.Headers[AdminKeyHeader].ToString();

        if (string.IsNullOrEmpty(adminKey) || providedKey != adminKey)
        {
            context.Result = new UnauthorizedObjectResult("Se requiere una clave de administrador.");
        }
    }
}
