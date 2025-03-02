namespace ArbitrageBot.BackgroundServices;

public abstract class BaseBackgroundService(IServiceProvider services, ILogger<BaseBackgroundService> logger) : BackgroundService
{
    // Could also be a async method, that can be awaited in ExecuteAsync above
    protected abstract Task DoWorkAsync(IServiceScope scope, CancellationToken cancellationToken);
    protected abstract TimeSpan TimerPeriod { get; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Timed Hosted Service running.");

        using PeriodicTimer timer = new(TimerPeriod);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                using (var scope = services.CreateScope())
                {
                    logger.LogInformation($"Service {GetType().Name} method DoWorkAsync is running.");
                    await DoWorkAsync(scope, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogError("Timed Hosted Service is stopping.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex.ToString());
        }
    }
}
