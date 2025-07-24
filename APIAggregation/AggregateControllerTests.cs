using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace APIAggregation;

public class AggregateControllerTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly IMemoryCache _memoryCache;
    private readonly Mock<ApiStatisticsService> _statsServiceMock;
    private readonly List<IApiProvider> _apiProviders;

    public AggregateControllerTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _statsServiceMock = new Mock<ApiStatisticsService>();

        // Fake provider that returns known value
        _apiProviders = new List<IApiProvider>
        {
            new FakeApiProvider("test", new { hello = "world" })
        };
    }

    [Fact]
    public async Task GetData_ReturnsAggregatedResult()
    {
        var controller = new AggregateController(
            _httpClientFactoryMock.Object,
            _memoryCache,
            _apiProviders,
            _statsServiceMock.Object);

        var result = await controller.GetData();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var data = Assert.IsAssignableFrom<IEnumerable<AggregatedData>>(okResult.Value);

        Assert.Single(data);
        Assert.Contains(data, d => d.Source == "test");
    }

    private class FakeApiProvider : IApiProvider
    {
        public string Name { get; }
        public string Url { get; }
        private readonly object _data;

        public FakeApiProvider(string name, object data)
        {
            Name = name;
            _data = data;
        }

        public Task<(object? Data, bool Success)> FetchAsync(HttpClient client)
        {
            return Task.FromResult(((object?)_data, true));
        }
    }
}