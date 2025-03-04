using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ArbitrageBot.BackgroundServices.Base;

// Extension methods for service registration
public static class BackgroundServiceHealthCheckExtensions
{
    public static IServiceCollection AddHealthTrackedBackgroundServices(this IServiceCollection services)
    {
        services.AddSingleton<BackgroundServiceRegistry>();

        // Register each of your background services
        services.AddHealthTrackedBackgroundService<AssetsBackgroundService>();

        return services;
    }

    public static IHealthChecksBuilder AddBackgroundServicesCheck(
        this IHealthChecksBuilder builder,
        string name = "background_services",
        HealthStatus? failureStatus = default,
        IEnumerable<string> tags = default,
        TimeSpan? staleExecutionThreshold = null)
    {
        return builder.AddCheck<BackgroundServicesHealthCheck>(
            name,
            failureStatus,
            tags,
            staleExecutionThreshold);
    }

    public static IServiceCollection AddHealthTrackedBackgroundService<TService>(
        this IServiceCollection services)
        where TService : BaseHealthTrackedBackgroundService
    {
        services.AddSingleton<TService>();

        services.AddHostedService(sp =>
        {
            var service = sp.GetRequiredService<TService>();
            var registry = sp.GetRequiredService<BackgroundServiceRegistry>();
            registry.RegisterService(service);
            return service;
        });

        return services;
    }
}