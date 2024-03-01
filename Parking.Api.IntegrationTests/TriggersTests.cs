namespace Parking.Api.IntegrationTests;

using System.Net.Http;
using System.Threading.Tasks;
using TestHelpers.Aws;
using Xunit;
using static Helpers.HttpClientHelpers;

[Collection("Database tests")]
public class TriggersTests(CustomWebApplicationFactory<Startup> factory) : IAsyncLifetime
{
    public async Task InitializeAsync() => await DatabaseHelpers.ResetDatabase();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Creates_recalculation_trigger()
    {
        var initialTriggerFileCount = await DatabaseHelpers.GetTriggerCount();

        var client = factory.CreateClient();

        AddAuthorizationHeader(client, UserType.Normal);

        await client.PostAsync("/triggers", new StringContent(string.Empty));

        var subsequentTriggerFileCount = await DatabaseHelpers.GetTriggerCount();

        Assert.Equal(0, initialTriggerFileCount);
        Assert.Equal(1, subsequentTriggerFileCount);
    }
}