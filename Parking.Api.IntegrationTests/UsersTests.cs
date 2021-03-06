namespace Parking.Api.IntegrationTests
{
    using System.Net;
    using System.Net.Http.Json;
    using System.Threading.Tasks;
    using Json.Users;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Xunit;
    using static HttpClientHelpers;

    [Collection("Database tests")]
    public class UsersTests : IAsyncLifetime
    {
        private readonly WebApplicationFactory<Startup> factory;

        public UsersTests(CustomWebApplicationFactory<Startup> factory) => this.factory = factory;

        public async Task InitializeAsync() => await DatabaseHelpers.ResetDatabase();

        public Task DisposeAsync() => Task.CompletedTask;

        [Theory]
        [InlineData(UserType.Normal)]
        [InlineData(UserType.TeamLeader)]
        public async Task Returns_forbidden_when_user_is_not_admin(UserType userType)
        {
            var client = this.factory.CreateClient();

            AddAuthorizationHeader(client, userType);

            var response = await client.GetAsync("/users");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Creates_new_user()
        {
            var client = this.factory.CreateClient();

            AddAuthorizationHeader(client, UserType.UserAdmin);

            var request = new UserPostRequest(
                "__ALTERNATIVE_REG__",
                12.3m, 
                "__EMAIL_ADDRESS__",
                "__FIRST_NAME__",
                "__LAST_NAME__",
                "__REG__");

            var postResult = await client.PostAsJsonAsync("/users", request);

            postResult.EnsureSuccessStatusCode();

            var savedUser = await DatabaseHelpers.ReadUser("NewUserId");

            Assert.Equal("__ALTERNATIVE_REG__", savedUser.AlternativeRegistrationNumber);
            Assert.Equal(12.3m, savedUser.CommuteDistance);
            Assert.Equal("__EMAIL_ADDRESS__", savedUser.EmailAddress);
            Assert.Equal("__FIRST_NAME__", savedUser.FirstName);
            Assert.Equal("__LAST_NAME__", savedUser.LastName);
            Assert.Equal("__REG__", savedUser.RegistrationNumber);
        }
    }
}