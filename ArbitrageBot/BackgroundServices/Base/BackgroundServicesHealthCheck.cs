using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ArbitrageBot.BackgroundServices.Base;

// The health check that monitors all registered services
public class BackgroundServicesHealthCheck : IHealthCheck
{
    private readonly BackgroundServiceRegistry _registry;
    private readonly TimeSpan _staleExecutionThreshold;

    public BackgroundServicesHealthCheck(
        BackgroundServiceRegistry registry,
        TimeSpan? staleExecutionThreshold = null)
    {
        _registry = registry;
        _staleExecutionThreshold = staleExecutionThreshold ?? TimeSpan.FromMinutes(15);
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var services = _registry.GetAllServices();

        if (!services.Any())
        {
            return Task.FromResult(HealthCheckResult.Degraded("No background services are registered."));
        }

        var unhealthyServices = new List<(string ServiceName, string Reason)>();
        var data = new Dictionary<string, object>();

        foreach (var service in services)
        {
            // Check if service is running
            if (!service.IsRunning)
            {
                unhealthyServices.Add((service.ServiceName, "Service is not running"));
                data.Add($"{service.ServiceName}_Status", "Not Running");
                continue;
            }

            // Check last error
            if (!string.IsNullOrEmpty(service.LastErrorMessage))
            {
                unhealthyServices.Add((service.ServiceName, $"Error: {service.LastErrorMessage}"));
                data.Add($"{service.ServiceName}_Error", service.LastErrorMessage);
            }

            // Check if service execution is stale
            var timeSinceLastExecution = DateTime.UtcNow - service.LastExecutionTime;
            if (timeSinceLastExecution > _staleExecutionThreshold)
            {
                unhealthyServices.Add((service.ServiceName, $"Stale execution: Last ran {timeSinceLastExecution.TotalMinutes:F1} minutes ago"));
                data.Add($"{service.ServiceName}_LastRun", $"{timeSinceLastExecution.TotalMinutes:F1} minutes ago");
            }

            // Add status data
            data.Add($"{service.ServiceName}_Status", "Running");
            data.Add($"{service.ServiceName}_LastExecutionTime", service.LastExecutionTime);
        }

        if (unhealthyServices.Any())
        {
            var description = string.Join(", ", unhealthyServices.Select(s => $"{s.ServiceName}: {s.Reason}"));
            return Task.FromResult(HealthCheckResult.Unhealthy(description, data: data));
        }

        return Task.FromResult(HealthCheckResult.Healthy("All background services are running", data));
    }
}
