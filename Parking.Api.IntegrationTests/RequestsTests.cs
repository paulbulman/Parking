namespace Parking.Api.IntegrationTests;

using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Json.Requests;
using NodaTime;
using NodaTime.Testing.Extensions;
using TestHelpers;
using TestHelpers.Aws;
using UnitTests.Json.Calendar;
using Xunit;
using static Helpers.HttpClientHelpers;

[Collection("Database tests")]
public class RequestsTests(CustomWebApplicationFactory<Startup> factory) : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await DatabaseHelpers.ResetDatabase();
        await NotificationHelpers.ResetNotifications();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData(UserType.Normal)]
    [InlineData(UserType.UserAdmin)]
    public async Task Get_by_user_returns_forbidden_when_user_is_not_team_leader(UserType userType)
    {
        var client = factory.CreateClient();

        AddAuthorizationHeader(client, userType);

        var response = await client.GetAsync("/requests/User1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(UserType.Normal)]
    [InlineData(UserType.UserAdmin)]
    public async Task Patch_by_user_returns_forbidden_when_user_is_not_team_leader(UserType userType)
    {
        var client = factory.CreateClient();

        AddAuthorizationHeader(client, userType);

        var request = new RequestsPatchRequest([]);

        var response = await client.PatchAsJsonAsync("/requests/User1", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Returns_existing_requests()
    {
        await DatabaseHelpers.CreateUser(CreateUser.With(userId: "User1"));

        var requests = new Dictionary<string, string>
        {
            {"01", "P"}, {"02", "A"}, {"03", "C"}, {"04", "S"}, {"05", "H"}, {"08", "I"}
        };

        await DatabaseHelpers.CreateRequests("User1", "2021-03", requests);

        var client = factory.CreateClient();

        AddAuthorizationHeader(client, UserType.Normal);

        var response = await client.GetAsync("/requests");

        response.EnsureSuccessStatusCode();

        var requestsResponse = await response.DeserializeAsType<RequestsResponse>();

        CheckReturnedRequest(requestsResponse, 01.March(2021), true);
        CheckReturnedRequest(requestsResponse, 02.March(2021), true);
        CheckReturnedRequest(requestsResponse, 03.March(2021), false);
        CheckReturnedRequest(requestsResponse, 04.March(2021), true);
        CheckReturnedRequest(requestsResponse, 05.March(2021), true);
        CheckReturnedRequest(requestsResponse, 08.March(2021), true);
        CheckReturnedRequest(requestsResponse, 09.March(2021), false);
    }

    [Fact]
    public async Task Returns_existing_requests_by_user_ID()
    {
        await DatabaseHelpers.CreateUser(CreateUser.With(userId: "User2"));

        var requests = new Dictionary<string, string> { { "01", "I" }, { "02", "A" } };

        await DatabaseHelpers.CreateRequests("User2", "2021-03", requests);

        var client = factory.CreateClient();

        AddAuthorizationHeader(client, UserType.TeamLeader);

        var response = await client.GetAsync("/requests/User2");

        response.EnsureSuccessStatusCode();

        var requestsResponse = await response.DeserializeAsType<RequestsResponse>();

        CheckReturnedRequest(requestsResponse, 01.March(2021), true);
        CheckReturnedRequest(requestsResponse, 02.March(2021), true);
        CheckReturnedRequest(requestsResponse, 03.March(2021), false);
    }

    [Fact]
    public async Task Saves_requests()
    {
        var client = factory.CreateClient();

        AddAuthorizationHeader(client, UserType.Normal);

        var request = new RequestsPatchRequest(
        [
            new RequestsPatchRequestDailyData(01.March(2021), true),
            new RequestsPatchRequestDailyData(03.March(2021), false)
        ]);

        await client.PatchAsJsonAsync("/requests", request);

        var savedRequests = await DatabaseHelpers.ReadRequests("User1", "2021-03");

        Assert.Equal(["01", "03"], savedRequests.Keys);

        Assert.Equal("P", savedRequests["01"]);
        Assert.Equal("C", savedRequests["03"]);
    }

    [Fact]
    public async Task Saves_requests_by_user_ID()
    {
        await DatabaseHelpers.CreateUser(CreateUser.With(userId: "User2"));

        var client = factory.CreateClient();

        AddAuthorizationHeader(client, UserType.TeamLeader);

        var request = new RequestsPatchRequest(
        [
            new RequestsPatchRequestDailyData(01.March(2021), true),
            new RequestsPatchRequestDailyData(03.March(2021), false)
        ]);

        await client.PatchAsJsonAsync("/requests/User2", request);

        var savedRequests = await DatabaseHelpers.ReadRequests("User2", "2021-03");

        Assert.Equal(["01", "03"], savedRequests.Keys);

        Assert.Equal("P", savedRequests["01"]);
        Assert.Equal("C", savedRequests["03"]);
    }

    [Fact]
    public async Task Creates_recalculation_trigger_after_saving()
    {
        var initialTriggerFileCount = await DatabaseHelpers.GetTriggerCount();

        var client = factory.CreateClient();

        AddAuthorizationHeader(client, UserType.Normal);

        var request = new RequestsPatchRequest([]);

        await client.PatchAsJsonAsync("/requests", request);

        var subsequentTriggerFileCount = await DatabaseHelpers.GetTriggerCount();

        Assert.Equal(0, initialTriggerFileCount);
        Assert.Equal(1, subsequentTriggerFileCount);
    }

    private static void CheckReturnedRequest(RequestsResponse response, LocalDate localDate, bool expectedValue)
    {
        var dailyData = CalendarHelpers.GetDailyData(response.Requests, localDate);

        Assert.Equal(expectedValue, dailyData.Requested);
    }
}