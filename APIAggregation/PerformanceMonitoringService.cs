namespace APIAggregation;

public class PerformanceMonitoringService : BackgroundService
{
    private readonly ILogger<PerformanceMonitoringService> _logger;
    private readonly ApiStatisticsService _stats;

    public PerformanceMonitoringService(ILogger<PerformanceMonitoringService> logger, ApiStatisticsService stats)
    {
        _logger = logger;
        _stats = stats;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var allTime = _stats.GetAllTimeAverages();
                var last5Min = _stats.Get5MinuteAverages();

                foreach (var (api, recentAvg) in last5Min)
                {
                    if (allTime.TryGetValue(api, out var overallAvg) && overallAvg > 0)
                    {
                        var ratio = recentAvg / overallAvg;
                        if (ratio > 1.5)
                        {
                            _logger.LogWarning("Performance anomaly detected on {Api}: 5min avg {RecentAvg}ms > 50% of overall avg {OverallAvg}ms", api, recentAvg, overallAvg);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while analyzing performance statistics.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}