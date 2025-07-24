using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Annotations;

namespace APIAggregation;

[ApiController]
[Route("api/[controller]")]
public class AggregateController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly IEnumerable<IApiProvider> _apiProviders;
    private readonly ApiStatisticsService _statsService;

    public AggregateController(IHttpClientFactory httpClientFactory, IMemoryCache cache,
        IEnumerable<IApiProvider> apiProviders, ApiStatisticsService statsService)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _apiProviders = apiProviders;
        _statsService = statsService;
    }

    /// <summary>
    /// Retrieves aggregated data from multiple external APIs.
    /// </summary>
    /// <param name="sortBy">Sort results by field (e.g., source).</param>
    /// <response code="200">Returns aggregated API data</response>
    [HttpGet("data")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<AggregatedData>), 200)]
    [SwaggerOperation(Summary = "Get aggregated data from all APIs.", Description = "Supports optional sorting by 'source'.")]
    
    public async Task<IActionResult> GetData([FromQuery] string? sortBy = null)
    {
        var client = _httpClientFactory.CreateClient("WithRetry");
        var tasks = _apiProviders.Select(async provider =>
        {
            var sw = Stopwatch.StartNew();
            bool success;
            if (_cache.TryGetValue(provider.Name, out var data))
            {
                success = true;
            }
            else
            {
                (data, success) = await provider.FetchAsync(client);
                if (success)
                    _cache.Set(provider.Name, data, TimeSpan.FromMinutes(5));
            }
            sw.Stop();
            _statsService.Record(provider.Name, sw.ElapsedMilliseconds);
            return new AggregatedData(provider.Name, data);
        });

        var results = await Task.WhenAll(tasks);
        IEnumerable<AggregatedData>? sorted=null;

        if (sortBy != null)
        {
            if (sortBy.Equals(nameof(AggregatedData.Source)))
            {
                sorted = results.OrderBy(r => r.Source);
            }
            return Ok(sorted);
        }

        return Ok(results);
    }

    /// <summary>
    /// Returns API call statistics including request count and average response time.
    /// </summary>
    /// <response code="200">Returns performance metrics per external API</response>
    [HttpGet("stats")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<ApiPerformance>), 200)]
    [SwaggerOperation(Summary = "Get API performance statistics.", Description = "Returns request count and average duration per API.")]
    public IActionResult GetStats()
    {
        return Ok(_statsService.GetStats());
    }
    
    /// <summary>
    /// Issues a JWT token for testing (no actual user validation).
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (request.Email != "admin@gmail.com" || request.Password != "password")
            return Unauthorized();

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes("super_secret_jwt_key_for_example");
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                new Claim(ClaimTypes.Name, request.Email)
            ]),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return Ok(new { token = tokenString });
    }
}