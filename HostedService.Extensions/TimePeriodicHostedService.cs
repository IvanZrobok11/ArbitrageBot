namespace HostedService.Extensions;

// Base class for background services
public abstract class TimePeriodicHostedService : BackgroundService
{
    protected readonly IServiceProvider _services;
    protected readonly ILogger _logger;
    protected readonly IServiceHealthManager? _healthManager;
    protected readonly string _serviceName;

    public TimePeriodicHostedService(IServiceProvider services, ILogger logger)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _healthManager = _services.GetService<IServiceHealthManager>();
        _serviceName = GetType().ToString();

        // Registration in health manager through interface
        _healthManager?.RegisterService(_serviceName);
    }

    // Method to be overridden in derived classes
    protected abstract Task DoWorkAsync(IServiceScope scope, CancellationToken cancellationToken);

    // Execution period
    protected abstract TimeSpan TimerPeriod { get; }

    // Run limit (unlimited by default)
    protected virtual long? RunsLimit { get; }

    // Run counter
    private long _runsCount = 0;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Service {ServiceName} started at: {time}", _serviceName, DateTimeOffset.Now);
        _healthManager?.UpdateServiceStatus(_serviceName, true, "Service started");

        using PeriodicTimer timer = new(TimerPeriod);

        try
        {
            do
            {
                if (RunsLimit.HasValue)
                {
                    if (_runsCount >= RunsLimit)
                    {
                        _logger.LogInformation("Reached run limit {Limit}, stopping service {ServiceName}.",
                            RunsLimit, _serviceName);
                        break;
                    }
                    Interlocked.Increment(ref _runsCount);
                }

                try
                {
                    using (var scope = _services.CreateScope())
                    {
                        _logger.LogDebug("Service {ServiceName} starts execution at: {time}",
                            _serviceName, DateTimeOffset.Now);

                        await DoWorkAsync(scope, stoppingToken);

                        _healthManager?.UpdateServiceStatus(_serviceName, true,
                            $"Task completed successfully (#{_runsCount})");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during execution of {ServiceName}", _serviceName);
                    _healthManager?.UpdateServiceStatus(_serviceName, true, $"An error occurred: {ex.Message}");
                }

            } while (!stoppingToken.IsCancellationRequested &&
                    await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Service {ServiceName} received stop signal", _serviceName);
            _healthManager?.UpdateServiceStatus(_serviceName, false, "Service canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error in service {ServiceName}", _serviceName);
            _healthManager?.UpdateServiceStatus(_serviceName, false, $"Critical error: {ex.Message}");
            throw;
        }
        finally
        {
            _logger.LogInformation("Service {ServiceName} stopped at: {time}", _serviceName, DateTimeOffset.Now);
        }
    }
}
