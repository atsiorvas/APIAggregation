using System.Text.Json;

namespace APIAggregation;

public class GitHubApiProvider : IApiProvider
{
    public string Name => "github";
    public string Url => "https://api.github.com/repos/dotnet/aspnetcore";
    public async Task<(object?, bool)> FetchAsync(HttpClient client)
    {
        try
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("HttpClientFactory-Sample");
            var result = await client.GetStringAsync(Url);
            var data = JsonSerializer.Deserialize<object>(result);
            return (data, true);
        }
        catch (Exception ex)
        {
            return ($"GitHub API failed: {ex.Message}", false);
        }
    }
}