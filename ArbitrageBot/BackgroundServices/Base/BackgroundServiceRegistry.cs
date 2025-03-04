namespace ArbitrageBot.BackgroundServices.Base;

// Service to track all registered health-tracked background services
public class BackgroundServiceRegistry
{
    private readonly List<BaseHealthTrackedBackgroundService> _services = new();

    public void RegisterService(BaseHealthTrackedBackgroundService service)
    {
        _services.Add(service);
    }

    public IReadOnlyList<BaseHealthTrackedBackgroundService> GetAllServices() => _services.AsReadOnly();
}
