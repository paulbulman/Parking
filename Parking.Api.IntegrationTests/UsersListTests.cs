namespace Parking.Api.IntegrationTests;

using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Json.UsersList;
using TestHelpers;
using TestHelpers.Aws;
using Xunit;
using static Helpers.HttpClientHelpers;

[Collection("Database tests")]
public class UsersListTests(CustomWebApplicationFactory<Startup> factory) : IAsyncLifetime
{
    public async Task InitializeAsync() => await DatabaseHelpers.ResetDatabase();

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData(UserType.Normal)]
    [InlineData(UserType.UserAdmin)]
    public async Task Returns_forbidden_when_user_is_not_team_leader(UserType userType)
    {
        await NotificationHelpers.ResetNotifications();

        var client = factory.CreateClient();

        AddAuthorizationHeader(client, userType);

        var response = await client.GetAsync("/usersList");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Returns_existing_users()
    {
        await DatabaseHelpers.CreateUser(
            CreateUser.With(userId: "User1", firstName: "Greer", lastName: "Lipsett"));
        await DatabaseHelpers.CreateUser(
            CreateUser.With(userId: "User2", firstName: "Chen", lastName: "Mesias"));

        var client = factory.CreateClient();

        AddAuthorizationHeader(client, UserType.TeamLeader);

        var response = await client.GetAsync("/usersList");

        response.EnsureSuccessStatusCode();

        var multipleUsersResponse = await response.DeserializeAsType<UsersListResponse>();

        var actualUsers = multipleUsersResponse.Users.ToArray();

        Assert.Equal(2, actualUsers.Length);

        Assert.Equal("User1", actualUsers[0].UserId);
        Assert.Equal("Greer Lipsett", actualUsers[0].Name);

        Assert.Equal("User2", actualUsers[1].UserId);
        Assert.Equal("Chen Mesias", actualUsers[1].Name);
    }
}