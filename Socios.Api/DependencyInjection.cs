using Microsoft.Extensions.DependencyInjection;
using Socios.Application.Interfaces;
using Socios.Application.Services;

namespace Socios.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Registramos el caso de uso principal del asistente
        services.AddScoped<IVirtualAssistantService, VirtualAssistantService>();
        services.AddScoped<IDocumentService, DocumentService>();

        // Usuarios y login del panel de administración
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<AdminAuthService>();
        services.AddScoped<AdminUserManagementService>();

        return services;
    }
}