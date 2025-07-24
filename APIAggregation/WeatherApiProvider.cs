using System.Text.Json;

namespace APIAggregation;

public class WeatherApiProvider : IApiProvider
{
    public string Name => "weather";
    public string Url => "https://api.open-meteo.com/v1/forecast?latitude=35&longitude=139&hourly=temperature_2m";
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
            return ($"Weather API failed: {ex.Message}", false);
        }
    }
}