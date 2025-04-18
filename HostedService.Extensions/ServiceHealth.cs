namespace HostedService.Extensions;

// Class for storing service health information
public class ServiceHealth
{
    public bool IsRunning { get; set; }
    public string LastStatusMessage { get; set; } = "Initialization...";
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    // Adding time to live for the record (24 hours by default)
    public TimeSpan TTL { get; set; } = TimeSpan.FromHours(24);

    // Method to check if the record is expired
    public bool IsExpired() => (DateTime.UtcNow - LastUpdated) > TTL;
}
