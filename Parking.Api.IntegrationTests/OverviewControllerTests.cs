// ReSharper disable StringLiteralTypo
namespace Parking.Api.IntegrationTests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Converters;
    using Json.Overview;
    using Microsoft.AspNetCore.Mvc.Testing;
    using NodaTime.Testing.Extensions;
    using UnitTests.Json.Calendar;
    using Xunit;

    public class OverviewControllerTests : IClassFixture<CustomWebApplicationFactory<Startup>>, IAsyncLifetime
    {
        private readonly WebApplicationFactory<Startup> factory;

        public OverviewControllerTests(CustomWebApplicationFactory<Startup> factory) => this.factory = factory;

        public async Task InitializeAsync() => await DatabaseHelpers.ResetDatabase();

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task Returns_success()
        {
            var client = this.factory.CreateClient();

            AddAuthorizationHeader(client);

            var response = await client.GetAsync("/overview");

            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task Returns_users_with_requests()
        {
            await DatabaseHelpers.CreateUser("User1", "Cash", "Meaders");
            await DatabaseHelpers.CreateUser("User2", "Kimball", "Ventom");

            var user1Requests = new Dictionary<string, string> { { "01", "R" }, { "02", "A" } };
            var user2Requests = new Dictionary<string, string> { { "02", "R" } };

            await DatabaseHelpers.CreateRequests("User1", "2021-03", user1Requests);
            await DatabaseHelpers.CreateRequests("User2", "2021-03", user2Requests);

            var client = this.factory.CreateClient();

            AddAuthorizationHeader(client);

            var response = await client.GetAsync("/overview");

            var responseContent = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            options.Converters.Add(new LocalDateConverter());

            var overviewResponse = JsonSerializer.Deserialize<OverviewResponse>(responseContent, options);

            Assert.NotNull(overviewResponse);

            var day1Data = CalendarHelpers.GetDailyData(overviewResponse.Overview, 1.March(2021));
            var day2Data = CalendarHelpers.GetDailyData(overviewResponse.Overview, 2.March(2021));

            Assert.Empty(day1Data.AllocatedUsers);
            Assert.Equal(new[] { "Cash Meaders" }, day1Data.InterruptedUsers.Select(u => u.Name));

            Assert.Equal(new[] { "Cash Meaders" }, day2Data.AllocatedUsers.Select(u => u.Name));
            Assert.Equal(new[] { "Kimball Ventom" }, day2Data.InterruptedUsers.Select(u => u.Name));
        }

        private static void AddAuthorizationHeader(HttpClient client)
        {
            const string RawTokenValue =
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9." +
                "eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ." +
                "SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

            client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {RawTokenValue}");
        }
    }
}