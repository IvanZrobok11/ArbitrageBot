using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HostedService.Extensions;

// HealthCheck for checking status of all services
public class MultipleServicesHealthCheck : IHealthCheck
{
    private readonly ILogger<MultipleServicesHealthCheck> _logger;
    private readonly IServiceHealthManager _healthManager;

    public MultipleServicesHealthCheck(
        ILogger<MultipleServicesHealthCheck> logger,
        IServiceHealthManager healthManager)
    {
        _logger = logger;
        _healthManager = healthManager;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var serviceStatuses = _healthManager.GetAllServiceStatuses();

            if (!serviceStatuses.Any())
            {
                _logger.LogWarning("No registered services found during health check.");
                return Task.FromResult(
                    HealthCheckResult.Degraded("No registered services found."));
            }

            var runningServices = serviceStatuses.Count(s => s.Value.IsRunning);
            var totalServices = serviceStatuses.Count;

            // Creating detailed information about each service
            var servicesInfo = new Dictionary<string, object>();
            foreach (var service in serviceStatuses)
            {
                servicesInfo.Add(service.Key, new
                {
                    Status = service.Value.IsRunning ? "Active" : "Inactive",
                    Message = service.Value.LastStatusMessage,
                    LastUpdated = service.Value.LastUpdated,
                    TTL = service.Value.TTL
                });
            }

            if (runningServices == totalServices)
            {
                _logger.LogDebug("Health Check: All services are running ({Running}/{Total}).",
                    runningServices, totalServices);

                return Task.FromResult(
                    HealthCheckResult.Healthy(
                        $"All services are running ({runningServices}/{totalServices}).",
                        data: servicesInfo));
            }
            else if (runningServices > 0)
            {
                _logger.LogWarning("Health Check: Some services are not running ({Running}/{Total}).",
                    runningServices, totalServices);

                return Task.FromResult(
                    HealthCheckResult.Degraded(
                        $"Some services are not running ({runningServices}/{totalServices}).",
                        data: servicesInfo));
            }
            else
            {
                _logger.LogWarning("Health Check: No services are running (0/{Total}).", totalServices);

                return Task.FromResult(
                    HealthCheckResult.Unhealthy(
                        "No services are running.",
                        data: servicesInfo));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Health Check execution");

            return Task.FromResult(
                HealthCheckResult.Unhealthy(
                    $"Error checking service status: {ex.Message}"));
        }
    }
}
