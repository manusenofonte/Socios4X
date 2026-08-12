using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Qdrant.Client;
using Socios.Application.Interfaces;
using Socios.Infrastructure.Persistence;
using Socios.Infrastructure.Repositories;

namespace Socios.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment) // Recibimos el environment para los checks de desarrollo
    {
        AddDatabase(services, configuration, environment);
        AddRepositories(services, configuration);

        return services;
    }

    private static void AddDatabase(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString("SociosDevConnection")
            ?? throw new InvalidOperationException("Connection string 'SociosDevConnection' not found.");

        // Usamos Pool para mejor performance
        services.AddDbContextPool<ClubDbContext>((serviceProvider, options) =>
        {
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

            options.UseSqlServer(connectionString, sql =>
            {
                // Resiliencia: Reintentos automáticos ante fallos transitorios
                sql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);
            });

            options.UseLoggerFactory(loggerFactory);

            if (environment.IsDevelopment())
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });
    }

    private static void AddRepositories(IServiceCollection services, IConfiguration configuration)
    {
        // Registro del repositorio de FAQs
        services.AddScoped<IFAQRepository, FAQRepository>();

        var qdrantEndpoint = configuration["AI:Qdrant:Endpoint"];
        if (string.IsNullOrWhiteSpace(qdrantEndpoint))
        {
            // Sin Qdrant configurado, el asistente sigue funcionando solo con FAQs
            services.AddScoped<IVectorKnowledgeRepository, NullVectorKnowledgeRepository>();
            return;
        }

        var qdrantApiKey = configuration["AI:Qdrant:ApiKey"];
        services.AddSingleton(_ => new QdrantClient(new Uri(qdrantEndpoint).Host, https: true, apiKey: qdrantApiKey));
        services.AddScoped<IVectorKnowledgeRepository, QdrantVectorKnowledgeRepository>();
    }
}