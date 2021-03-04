namespace Parking.Api.IntegrationTests
{
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Xunit;

    public class StatusControllerTests : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly WebApplicationFactory<Startup> factory;

        public StatusControllerTests(WebApplicationFactory<Startup> factory) => this.factory = factory;

        [Fact]
        public async Task Returns_success()
        {
            const string RawTokenValue =
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9." +
                "eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ." +
                "SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

            var client = this.factory.CreateClient();

            client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {RawTokenValue}");

            var response = await client.GetAsync("/status");

            response.EnsureSuccessStatusCode();
        }
    }
}
