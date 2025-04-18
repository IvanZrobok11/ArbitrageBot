namespace HostedService.Extensions;

/// <summary>
/// Interface for managing service health
/// </summary>
public interface IServiceHealthManager : IDisposable
{
    /// <summary>
    /// Registers service for monitoring
    /// </summary>
    /// <param name="serviceName">Service identifier</param>
    /// <param name="ttl">Time to live for the record (optional)</param>
    void RegisterService(string serviceName, TimeSpan? ttl = null);

    /// <summary>
    /// Updates service status
    /// </summary>
    /// <param name="serviceName">Service identifier</param>
    /// <param name="isRunning">Whether the service is running</param>
    /// <param name="message">Additional message (optional)</param>
    /// <param name="ttl">Time to live for the record (optional)</param>
    void UpdateServiceStatus(string serviceName, bool isRunning, string message = null, TimeSpan? ttl = null);

    /// <summary>
    /// Gets statuses of all registered services
    /// </summary>
    /// <returns>Dictionary with service statuses</returns>
    Dictionary<string, ServiceHealth> GetAllServiceStatuses();

    /// <summary>
    /// Checks if a specific service is running
    /// </summary>
    /// <param name="serviceName">Service identifier</param>
    /// <returns>true if the service is running; false if not or not registered</returns>
    bool IsServiceRunning(string serviceName);

    /// <summary>
    /// Starts cleanup of expired service records
    /// </summary>
    void CleanupExpiredServices();
}
