namespace Parking.Api.IntegrationTests;

using System.Threading.Tasks;
using Xunit;
using static Helpers.HttpClientHelpers;

[Collection("Database tests")]
public class StatusTests(CustomWebApplicationFactory<Startup> factory)
{

    [Fact]
    public async Task Returns_success()
    {
        var client = factory.CreateClient();

        AddAuthorizationHeader(client, UserType.Normal);

        var response = await client.GetAsync("/status", TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
    }
}