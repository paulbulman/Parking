namespace Parking.Api.IntegrationTests;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using static Helpers.HttpClientHelpers;

public class StatusTests : IClassFixture<CustomWebApplicationFactory<Startup>>
{
    private readonly WebApplicationFactory<Startup> factory;

    public StatusTests(CustomWebApplicationFactory<Startup> factory) => this.factory = factory;

    [Fact]
    public async Task Returns_success()
    {
        var client = this.factory.CreateClient();

        AddAuthorizationHeader(client, UserType.Normal);

        var response = await client.GetAsync("/status");

        response.EnsureSuccessStatusCode();
    }
}