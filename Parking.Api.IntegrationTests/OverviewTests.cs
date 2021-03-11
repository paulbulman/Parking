// ReSharper disable StringLiteralTypo
namespace Parking.Api.IntegrationTests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Helpers;
    using Json.Overview;
    using Microsoft.AspNetCore.Mvc.Testing;
    using NodaTime.Testing.Extensions;
    using TestHelpers;
    using UnitTests.Json.Calendar;
    using Xunit;
    using static Helpers.HttpClientHelpers;

    [Collection("Database tests")]
    public class OverviewTests : IAsyncLifetime
    {
        private readonly WebApplicationFactory<Startup> factory;

        public OverviewTests(CustomWebApplicationFactory<Startup> factory) => this.factory = factory;

        public async Task InitializeAsync() => await DatabaseHelpers.ResetDatabase();

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task Returns_users_with_requests()
        {
            await SeedDatabase();

            var client = this.factory.CreateClient();

            AddAuthorizationHeader(client, UserType.Normal);

            var response = await client.GetAsync("/overview");

            response.EnsureSuccessStatusCode();

            var overviewResponse = await response.DeserializeAsType<OverviewResponse>();

            var day1Data = CalendarHelpers.GetDailyData(overviewResponse.Overview, 1.March(2021));
            var day2Data = CalendarHelpers.GetDailyData(overviewResponse.Overview, 2.March(2021));

            Assert.Empty(day1Data.AllocatedUsers);
            Assert.Equal(new[] { "Cash Meaders" }, day1Data.InterruptedUsers.Select(u => u.Name));

            Assert.Equal(new[] { "Cash Meaders" }, day2Data.AllocatedUsers.Select(u => u.Name));
            Assert.Equal(new[] { "Kimball Ventom" }, day2Data.InterruptedUsers.Select(u => u.Name));
        }

        private static async Task SeedDatabase()
        {
            await DatabaseHelpers.CreateUser(CreateUser.With(userId: "User1", firstName: "Cash", lastName: "Meaders"));
            await DatabaseHelpers.CreateUser(CreateUser.With(userId: "User2", firstName: "Kimball", lastName: "Ventom"));

            var user1Requests = new Dictionary<string, string> {{"01", "R"}, {"02", "A"}};
            var user2Requests = new Dictionary<string, string> {{"02", "R"}};

            await DatabaseHelpers.CreateRequests("User1", "2021-03", user1Requests);
            await DatabaseHelpers.CreateRequests("User2", "2021-03", user2Requests);
        }
    }
}