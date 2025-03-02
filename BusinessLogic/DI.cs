using BusinessLogic.Interfaces;
using BusinessLogic.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace BusinessLogic;

public static class ServiceCollectionExtensions
{
    public static void AddCryptoApiServices(this IServiceCollection services)
    {
        services.AddImplementationsByBase<ICryptoExchangeApiService>(ServiceLifetime.Scoped);
        services.AddScoped<CommonExchangeService>();
    }

    public static void AddImplementationsByBase<TService>(this IServiceCollection services, ServiceLifetime lifetime)
    {
        var serviceType = typeof(TService);
        var implementations = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => serviceType.IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false });

        foreach (var implementation in implementations)
        {
            switch (lifetime)
            {
                case ServiceLifetime.Singleton:
                    services.AddSingleton(serviceType, implementation);
                    break;
                case ServiceLifetime.Scoped:
                    services.AddScoped(serviceType, implementation);
                    break;
                case ServiceLifetime.Transient:
                    services.AddTransient(serviceType, implementation);
                    break;
                default:
                    break;
            }
        }
    }
}
