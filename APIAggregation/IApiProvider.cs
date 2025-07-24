namespace APIAggregation;

public interface IApiProvider
{
    string Name { get; }
    string Url { get; }
    Task<(object? Data, bool Success)> FetchAsync(HttpClient client);
}