namespace APIAggregation;

public class ApiPerformance
{
    public string Api { get; set; } = default!;
    public int TotalRequests { get; set; }
    public double AvgResponseTimeMs { get; set; }
    public string PerformanceBucket { get; set; } = default!;
}