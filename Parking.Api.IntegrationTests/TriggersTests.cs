namespace Parking.Api.IntegrationTests;

using System.Net.Http;
using System.Threading.Tasks;
using TestHelpers.Aws;
using Xunit;
using static Helpers.HttpClientHelpers;

[Collection("Database tests")]
public class TriggersTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory<Startup> factory;

    public TriggersTests(CustomWebApplicationFactory<Startup> factory) => this.factory = factory;

    public async Task InitializeAsync() => await DatabaseHelpers.ResetDatabase();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Creates_recalculation_trigger()
    {
        var initialTriggerFileCount = await DatabaseHelpers.GetTriggerCount();

        var client = this.factory.CreateClient();

        AddAuthorizationHeader(client, UserType.Normal);

        await client.PostAsync("/triggers", new StringContent(string.Empty));

        var subsequentTriggerFileCount = await DatabaseHelpers.GetTriggerCount();

        Assert.Equal(0, initialTriggerFileCount);
        Assert.Equal(1, subsequentTriggerFileCount);
    }
}