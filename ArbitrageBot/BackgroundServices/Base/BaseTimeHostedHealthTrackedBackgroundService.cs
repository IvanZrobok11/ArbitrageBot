namespace ArbitrageBot.BackgroundServices.Base;

public abstract class BaseTimeHostedHealthTrackedBackgroundService(IServiceProvider services, ILogger<BaseTimeHostedHealthTrackedBackgroundService> logger) : BaseHealthTrackedBackgroundService
{
    // Could also be a async method, that can be awaited in ExecuteAsync above
    protected abstract Task DoWorkAsync(IServiceScope scope, CancellationToken cancellationToken);
    protected abstract TimeSpan TimerPeriod { get; }

    protected override async Task ExecuteTrackedAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Timed Hosted Service running.");

        using PeriodicTimer timer = new(TimerPeriod);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            logger.LogInformation($"Service {GetType().Name} method ExecuteTrackedAsync is running.");
            await TrackExecution(async () =>
            {
                using (var scope = services.CreateScope())
                {
                    await DoWorkAsync(scope, stoppingToken);
                }
            });
        }
    }
}
