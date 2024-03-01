// ReSharper disable StringLiteralTypo
namespace Parking.Api.IntegrationTests;

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Json.Reservations;
using NodaTime.Testing.Extensions;
using TestHelpers;
using TestHelpers.Aws;
using UnitTests.Json.Calendar;
using Xunit;
using static Helpers.HttpClientHelpers;

[Collection("Database tests")]
public class ReservationsTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory<Startup> factory;

    public ReservationsTests(CustomWebApplicationFactory<Startup> factory) => this.factory = factory;

    public async Task InitializeAsync()
    {
        await DatabaseHelpers.ResetDatabase();
        await NotificationHelpers.ResetNotifications();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData(UserType.Normal)]
    [InlineData(UserType.UserAdmin)]
    public async Task Get_returns_forbidden_when_user_is_not_team_leader(UserType userType)
    {
        var client = this.factory.CreateClient();

        AddAuthorizationHeader(client, userType);

        var response = await client.GetAsync("/reservations");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(UserType.Normal)]
    [InlineData(UserType.UserAdmin)]
    public async Task Patch_returns_forbidden_when_user_is_not_team_leader(UserType userType)
    {
        var client = this.factory.CreateClient();

        AddAuthorizationHeader(client, userType);

        var request = new ReservationsPatchRequest(new List<ReservationsPatchRequestDailyData>());

        var response = await client.PatchAsJsonAsync("/reservations", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Returns_existing_reservations()
    {
        await DatabaseHelpers.CreateUser(CreateUser.With(userId: "User1", firstName: "Thornie", lastName: "Newis"));
        await DatabaseHelpers.CreateUser(CreateUser.With(userId: "User2", firstName: "Sully", lastName: "Paolino"));
        await DatabaseHelpers.CreateDeletedUser(CreateUser.With(userId: "User3"));

        await DatabaseHelpers.CreateReservations("2021-03",
            new Dictionary<string, List<string>>
            {
                {"01", new List<string> {"User1", "User2"}},
                {"02", new List<string> {"User2", "User3"}}
            });

        await DatabaseHelpers.CreateConfiguration(new Dictionary<string, string>
        {
            {"shortLeadTimeSpaces", "2"},
            {"totalSpaces", "9"},
            {"nearbyDistance", "1.5"}
        });

        var client = this.factory.CreateClient();

        AddAuthorizationHeader(client, UserType.TeamLeader);

        var response = await client.GetAsync("/reservations");

        response.EnsureSuccessStatusCode();

        var reservationsResponse = await response.DeserializeAsType<ReservationsResponse>();

        var day1Data = CalendarHelpers.GetDailyData(reservationsResponse.Reservations, 1.March(2021));
        var day2Data = CalendarHelpers.GetDailyData(reservationsResponse.Reservations, 2.March(2021));

        Assert.Equal(new[] { "User1", "User2" }, day1Data.UserIds);
        Assert.Equal(new[] { "User2" }, day2Data.UserIds);

        Assert.Equal(2, reservationsResponse.ShortLeadTimeSpaces);

        var actualUsers = reservationsResponse.Users.ToArray();

        Assert.Equal(2, actualUsers.Length);

        Assert.Equal("User1", actualUsers[0].UserId);
        Assert.Equal("Thornie Newis", actualUsers[0].Name);

        Assert.Equal("User2", actualUsers[1].UserId);
        Assert.Equal("Sully Paolino", actualUsers[1].Name);
    }

    [Fact]
    public async Task Saves_new_reservations()
    {
        var client = this.factory.CreateClient();

        AddAuthorizationHeader(client, UserType.TeamLeader);

        var request = new ReservationsPatchRequest(new[]
        {
            new ReservationsPatchRequestDailyData(1.March(2021), new[] {"User3", "User4"}),
            new ReservationsPatchRequestDailyData(2.March(2021), new[] {"User4", "User5"})
        });

        await client.PatchAsJsonAsync("/reservations", request);

        var savedReservations = await DatabaseHelpers.ReadReservations("2021-03");

        Assert.Equal(new[] { "01", "02" }, savedReservations.Keys);

        Assert.Equal(new[] { "User3", "User4" }, savedReservations["01"]);
        Assert.Equal(new[] { "User4", "User5" }, savedReservations["02"]);
    }
}