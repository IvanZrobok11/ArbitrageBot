using System.Collections.Concurrent;

namespace HostedService.Extensions;

/// <summary>
/// Implementation of manager for managing service health
/// </summary>
public class ServiceHealthManager : IServiceHealthManager
{
    // Internal storage for service statuses (ConcurrentDictionary for thread safety)
    private readonly ConcurrentDictionary<string, ServiceHealth> _serviceStatuses = new();

    // Logger
    private readonly ILogger<ServiceHealthManager> _logger;

    // Timer for periodic cleanup of expired records
    private readonly Timer _cleanupTimer;

    // By default we check expired records every hour
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(1);

    // Using public constructor for DI
    public ServiceHealthManager(ILogger<ServiceHealthManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize timer for cleanup
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, _cleanupInterval, _cleanupInterval);

        _logger.LogInformation("Service health manager initialized");
    }

    // Method for cleaning up expired records
    private void CleanupExpiredEntries(object state)
    {
        try
        {
            int removedCount = 0;

            // Find all keys of expired records
            var expiredKeys = _serviceStatuses
                .Where(kvp => kvp.Value.IsExpired())
                .Select(kvp => kvp.Key)
                .ToList();

            // Remove expired records
            foreach (var key in expiredKeys)
            {
                if (_serviceStatuses.TryRemove(key, out var removedService))
                {
                    removedCount++;
                    _logger.LogInformation("Removed expired service record {ServiceName}. " +
                        "Last update: {LastUpdated}, TTL: {TTL}",
                        key, removedService.LastUpdated, removedService.TTL);
                }
            }

            if (removedCount > 0)
            {
                _logger.LogInformation("Cleaned up {Count} expired service records", removedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired service records");
        }
    }

    // Service registration
    public void RegisterService(string serviceName, TimeSpan? ttl = null)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            throw new ArgumentException("Service name cannot be empty", nameof(serviceName));

        // ConcurrentDictionary has a thread-safe GetOrAdd method
        _serviceStatuses.GetOrAdd(serviceName, _ =>
        {
            var serviceHealth = new ServiceHealth { IsRunning = false };

            // If TTL is provided, set it
            if (ttl.HasValue)
            {
                serviceHealth.TTL = ttl.Value;
            }

            _logger.LogInformation("Service {ServiceName} registered in monitoring system with TTL: {TTL}",
                serviceName, serviceHealth.TTL);

            return serviceHealth;
        });
    }

    // Updating service status
    public void UpdateServiceStatus(string serviceName, bool isRunning, string message = null, TimeSpan? ttl = null)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            throw new ArgumentException("Service name cannot be empty", nameof(serviceName));

        // Add or update service status
        _serviceStatuses.AddOrUpdate(
            serviceName,
            // Function to create a new item if key doesn't exist
            _ =>
            {
                var serviceHealth = new ServiceHealth
                {
                    IsRunning = isRunning,
                    LastStatusMessage = message ?? (isRunning ? "Running normally" : "Not active"),
                    LastUpdated = DateTime.UtcNow
                };

                // If TTL is provided, set it
                if (ttl.HasValue)
                {
                    serviceHealth.TTL = ttl.Value;
                }

                _logger.LogDebug("Automatically registered and updated service status {ServiceName}: {Status}, {Message}, TTL: {TTL}",
                    serviceName, isRunning ? "Active" : "Inactive", message, serviceHealth.TTL);

                return serviceHealth;
            },
            // Function to update existing item
            (_, existingHealth) =>
            {
                existingHealth.IsRunning = isRunning;
                existingHealth.LastStatusMessage = message ?? (isRunning ? "Running normally" : "Not active");
                existingHealth.LastUpdated = DateTime.UtcNow;

                // If TTL is provided, update it
                if (ttl.HasValue)
                {
                    existingHealth.TTL = ttl.Value;
                }

                _logger.LogDebug("Updated service status {ServiceName}: {Status}, {Message}, TTL: {TTL}",
                    serviceName, isRunning ? "Active" : "Inactive", message, existingHealth.TTL);

                return existingHealth;
            }
        );
    }

    // Getting statuses of all services
    public Dictionary<string, ServiceHealth> GetAllServiceStatuses()
    {
        // Convert ConcurrentDictionary to regular Dictionary
        return _serviceStatuses.ToDictionary(entry => entry.Key, entry => entry.Value);
    }

    // Checking if a specific service is running
    public bool IsServiceRunning(string serviceName)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            throw new ArgumentException("Service name cannot be empty", nameof(serviceName));

        return _serviceStatuses.TryGetValue(serviceName, out var status) && status.IsRunning;
    }

    // Manual cleanup start
    public void CleanupExpiredServices()
    {
        CleanupExpiredEntries(null);
    }

    // IDisposable implementation
    public void Dispose()
    {
        _cleanupTimer?.Dispose();
        _logger.LogInformation("Service health manager disposed");
    }
}
