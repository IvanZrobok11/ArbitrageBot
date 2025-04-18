namespace HostedService.Extensions;

public static class HealthCheckBuilderExtensions
{
    public static IHealthChecksBuilder AddBackgroundServicesCheck(this IHealthChecksBuilder builder)
    {
        // Registration of IServiceHealthManager in DI as singleton
        builder.Services.AddSingleton<IServiceHealthManager, ServiceHealthManager>();

        return builder.AddCheck<MultipleServicesHealthCheck>("background_services_check");
    }
}
