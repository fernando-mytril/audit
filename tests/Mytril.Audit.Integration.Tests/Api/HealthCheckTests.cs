using System.Net;
using Mytril.Audit.Integration.Tests.Fixtures;

namespace Mytril.Audit.Integration.Tests.Api;

public sealed class HealthCheckTests(AuditWebApplicationFactory factory)
    : IClassFixture<AuditWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task HealthLive_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("Healthy", body);
    }

    [Fact]
    public async Task HealthReady_ReturnsOk()
    {
        var response = await _client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
