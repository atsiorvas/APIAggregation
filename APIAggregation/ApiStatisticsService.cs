using System.Collections.Concurrent;

namespace APIAggregation;

public class ApiStatisticsService
{
    private readonly ConcurrentDictionary<string, List<(DateTime Timestamp, long Duration)>> _data = new();

    public void Record(string api, long durationMs)
    {
        var entry = (DateTime.UtcNow, durationMs);
        _data.AddOrUpdate(api,
            _ => [entry],
            (_, list) => {
                lock (list)
                {
                    list.Add(entry);
                    list.RemoveAll(x => x.Timestamp < DateTime.UtcNow.AddMinutes(-10));
                    return list;
                }
            });
    }

    public IEnumerable<ApiPerformance> GetStats()
    {
        foreach (var (api, times) in _data)
        {
            var durations = times.Select(x => x.Duration).ToList();
            if (!durations.Any()) continue;

            double avg = durations.Average();
            string bucket = avg switch
            {
                < 100 => "fast",
                < 200 => "average",
                _ => "slow"
            };

            yield return new ApiPerformance
            {
                Api = api,
                TotalRequests = durations.Count,
                AvgResponseTimeMs = avg,
                PerformanceBucket = bucket
            };
        }
    }

    public Dictionary<string, double> Get5MinuteAverages()
    {
        var result = new Dictionary<string, double>();
        foreach (var (api, entries) in _data)
        {
            var recent = entries.Where(x => x.Timestamp >= DateTime.UtcNow.AddMinutes(-5)).Select(x => x.Duration).ToList();
            if (recent.Count > 0)
                result[api] = recent.Average();
        }
        return result;
    }

    public Dictionary<string, double> GetAllTimeAverages()
    {
        var result = new Dictionary<string, double>();
        foreach (var (api, entries) in _data)
        {
            if (entries.Count > 0)
                result[api] = entries.Average(x => x.Duration);
        }
        return result;
    }
}