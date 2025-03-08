namespace ArbitrageBot.BackgroundServices.Base;

// Base BackgroundService that includes health tracking
public abstract class BaseHealthTrackedBackgroundService : BackgroundService
{
    protected BaseHealthTrackedBackgroundService()
    {
        _lastExecutionTime = DateTime.UtcNow;
    }

    private volatile bool _isRunning = false;
    protected volatile string? _lastErrorMessage = null;
    private DateTime _lastExecutionTime;

    public bool IsRunning => _isRunning;
    public string? LastErrorMessage => _lastErrorMessage;
    public DateTime LastExecutionTime => _lastExecutionTime;
    public string ServiceName => GetType().Name;
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _isRunning = true;
        _lastErrorMessage = null;

        try
        {
            await ExecuteTrackedAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _isRunning = false;
            _lastErrorMessage = ex.Message;
            throw;
        }
        finally
        {
            _isRunning = false;
        }
    }

    // Method to track execution time
    protected async Task TrackExecution(Func<Task> action)
    {
        _lastExecutionTime = DateTime.UtcNow;
        await action();
    }

    // Abstract method that must be implemented by derived classes
    protected abstract Task ExecuteTrackedAsync(CancellationToken stoppingToken);
}