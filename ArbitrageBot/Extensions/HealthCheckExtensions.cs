using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics;
using System.Text.Json;
namespace ArbitrageBot.Extensions;

public static class HealthCheckExtensions
{
    public static IEndpointRouteBuilder MapHealthCheckEndpoint(this IEndpointRouteBuilder endpoints, string path = "/health")
    {
        endpoints.MapGet(path, async (HttpContext context, HealthCheckService healthCheckService) =>
        {
            var report = await healthCheckService.CheckHealthAsync();
            context.Response.ContentType = "application/json";

            var result = JsonSerializer.Serialize(new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description
                })
            }, new JsonSerializerOptions { WriteIndented = true });

            await context.Response.WriteAsync(result);
        });

        return endpoints;
    }

    public static IHealthChecksBuilder AddMemoryHealthCheck(this IHealthChecksBuilder builder)
    {
        return builder.AddCheck("Memory", () =>
        {
            var memoryInfo = GC.GetGCMemoryInfo();
            var status = memoryInfo.TotalAvailableMemoryBytes < 1024 * 1024 * 100
                ? HealthStatus.Degraded
                : HealthStatus.Healthy;

            return new HealthCheckResult(
                status,
                description: $"Memory usage: {Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024)}MB"
            );
        });
    }
}
