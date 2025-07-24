using System.Text.Json;

namespace APIAggregation;

public class NewsApiProvider : IApiProvider
{
    public string Name => "news";
    public string Url => "https://newsapi.org/v2/top-headlines?country=us&apiKey=demo";
    public async Task<(object?, bool)> FetchAsync(HttpClient client)
    {
        try
        {
            var result = await client.GetStringAsync(Url);
            var data = JsonSerializer.Deserialize<object>(result);
            return (data, true);
        }
        catch (Exception ex)
        {
            return ($"News API failed: {ex.Message}", false);
        }
    }
}