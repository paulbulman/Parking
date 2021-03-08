﻿namespace Parking.Api.IntegrationTests
{
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Json.UsersList;
    using TestHelpers;
    using Xunit;
    using static HttpClientHelpers;

    [Collection("Database tests")]
    public class UsersListTests : IAsyncLifetime
    {
        private readonly CustomWebApplicationFactory<Startup> factory;

        public UsersListTests(CustomWebApplicationFactory<Startup> factory) => this.factory = factory;

        public async Task InitializeAsync() => await DatabaseHelpers.ResetDatabase();

        public Task DisposeAsync() => Task.CompletedTask;

        [Theory]
        [InlineData(UserType.Normal)]
        [InlineData(UserType.UserAdmin)]
        public async Task Returns_forbidden_when_user_is_not_team_leader(UserType userType)
        {
            await NotificationHelpers.ResetNotifications();

            var client = this.factory.CreateClient();

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

            var client = this.factory.CreateClient();

            AddAuthorizationHeader(client, UserType.TeamLeader);

            var response = await client.GetAsync("/usersList");

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();

            var multipleUsersResponse = JsonHelpers.Deserialize<UsersListResponse>(responseContent);

            var actualUsers = multipleUsersResponse.Users.ToArray();

            Assert.Equal(2, actualUsers.Length);

            Assert.Equal("User1", actualUsers[0].UserId);
            Assert.Equal("Greer Lipsett", actualUsers[0].Name);

            Assert.Equal("User2", actualUsers[1].UserId);
            Assert.Equal("Chen Mesias", actualUsers[1].Name);
        }
    }
}