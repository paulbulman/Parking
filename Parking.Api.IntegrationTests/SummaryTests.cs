namespace Parking.Api.IntegrationTests
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Json.Summary;
    using Microsoft.AspNetCore.Mvc.Testing;
    using NodaTime.Testing.Extensions;
    using TestHelpers;
    using TestHelpers.Aws;
    using UnitTests.Json.Calendar;
    using Xunit;
    using static Helpers.HttpClientHelpers;

    [Collection("Database tests")]
    public class SummaryTests : IAsyncLifetime
    {
        private readonly WebApplicationFactory<Startup> factory;

        public SummaryTests(CustomWebApplicationFactory<Startup> factory) => this.factory = factory;

        public async Task InitializeAsync() => await DatabaseHelpers.ResetDatabase();

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task Returns_statuses()
        {
            await SeedDatabase();

            var client = this.factory.CreateClient();

            AddAuthorizationHeader(client, UserType.Normal);

            var response = await client.GetAsync("/summary");

            response.EnsureSuccessStatusCode();

            var summaryResponse = await response.DeserializeAsType<SummaryResponse>();

            var day1Data = CalendarHelpers.GetDailyData(summaryResponse.Summary, 1.March(2021));
            var day2Data = CalendarHelpers.GetDailyData(summaryResponse.Summary, 2.March(2021));
            var day3Data = CalendarHelpers.GetDailyData(summaryResponse.Summary, 3.March(2021));
            var day31Data = CalendarHelpers.GetDailyData(summaryResponse.Summary, 31.March(2021));

            Assert.Equal(SummaryStatus.Interrupted, day1Data.Status);
            Assert.True(day1Data.IsProblem);

            Assert.Equal(SummaryStatus.Allocated, day2Data.Status);
            Assert.False(day2Data.IsProblem);

            Assert.Null(day3Data.Status);
            Assert.False(day3Data.IsProblem);

            Assert.Equal(SummaryStatus.Requested, day31Data.Status);
            Assert.False(day31Data.IsProblem);
        }

        private static async Task SeedDatabase()
        {
            await DatabaseHelpers.CreateUser(CreateUser.With(userId: "User1"));

            var user1Requests = new Dictionary<string, string> { { "01", "R" }, { "02", "A" }, { "31", "R" } };

            await DatabaseHelpers.CreateRequests("User1", "2021-03", user1Requests);
        }
    }
}