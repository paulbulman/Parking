// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local
namespace Parking.Api.IntegrationTests
{
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Json.Users;
    using Microsoft.AspNetCore.Mvc.Testing;
    using TestHelpers;
    using TestHelpers.Aws;
    using Xunit;
    using static Helpers.HttpClientHelpers;

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
            await NotificationHelpers.ResetNotifications();

            var client = this.factory.CreateClient();

            AddAuthorizationHeader(client, userType);

            var response = await client.GetAsync("/users");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Returns_existing_users()
        {
            await DatabaseHelpers.CreateUser(
                CreateUser.With(
                    userId: "User1",
                    alternativeRegistrationNumber: "A123ABC",
                    commuteDistance: 11,
                    firstName: "Carolan",
                    lastName: "Mussalli",
                    registrationNumber: "AB12CDE"));
            await DatabaseHelpers.CreateUser(
                CreateUser.With(
                    userId: "User2",
                    alternativeRegistrationNumber: "X789XYZ",
                    commuteDistance: 22,
                    firstName: "Ethelyn",
                    lastName: "Salamana",
                    registrationNumber: "XY89XYZ"));
            await DatabaseHelpers.CreateDeletedUser(CreateUser.With(userId: "User3"));

            var client = this.factory.CreateClient();

            AddAuthorizationHeader(client, UserType.UserAdmin);

            var response = await client.GetAsync("/users");

            response.EnsureSuccessStatusCode();

            var multipleUsersResponse = await response.DeserializeAsType<MultipleUsersResponse>();

            var actualUsers = multipleUsersResponse.Users.ToArray();

            Assert.Equal(2, actualUsers.Length);

            CheckUser(actualUsers[0], "User1", "A123ABC", 11, "Carolan", "Mussalli", "AB12CDE");
            CheckUser(actualUsers[1], "User2", "X789XYZ", 22, "Ethelyn", "Salamana", "XY89XYZ");
        }

        [Fact]
        public async Task Returns_existing_user()
        {
            await DatabaseHelpers.CreateUser(
                CreateUser.With(
                    userId: "User2",
                    alternativeRegistrationNumber: "X789XYZ",
                    commuteDistance: 22,
                    firstName: "Ethelyn",
                    lastName: "Salamana",
                    registrationNumber: "XY89XYZ"));

            var client = this.factory.CreateClient();

            AddAuthorizationHeader(client, UserType.UserAdmin);

            var response = await client.GetAsync("/users/User2");

            response.EnsureSuccessStatusCode();

            var multipleUsersResponse = await response.DeserializeAsType<SingleUserResponse>();

            var actualUser = multipleUsersResponse.User;

            CheckUser(actualUser, "User2", "X789XYZ", 22, "Ethelyn", "Salamana", "XY89XYZ");
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

            var response = await client.PostAsJsonAsync("/users", request);

            response.EnsureSuccessStatusCode();

            var savedUser = await DatabaseHelpers.ReadUser("NewUserId");

            Assert.Equal("__ALTERNATIVE_REG__", savedUser.AlternativeRegistrationNumber);
            Assert.Equal(12.3m, savedUser.CommuteDistance);
            Assert.Equal("__EMAIL_ADDRESS__", savedUser.EmailAddress);
            Assert.Equal("__FIRST_NAME__", savedUser.FirstName);
            Assert.Equal("__LAST_NAME__", savedUser.LastName);
            Assert.Equal("__REG__", savedUser.RegistrationNumber);
        }

        [Fact]
        public async Task Updates_existing_user()
        {
            await DatabaseHelpers.CreateUser(
                CreateUser.With(
                    userId: "User2",
                    alternativeRegistrationNumber: "X789XYZ",
                    commuteDistance: 22,
                    firstName: "Ethelyn",
                    lastName: "Salamana",
                    registrationNumber: "XY89XYZ"));
            
            var client = this.factory.CreateClient();

            AddAuthorizationHeader(client, UserType.UserAdmin);

            var request = new UserPatchRequest(
                "__ALTERNATIVE_REG__",
                12.3m,
                "__FIRST_NAME__",
                "__LAST_NAME__",
                "__REG__");

            var response = await client.PatchAsJsonAsync("/users/User2", request);

            response.EnsureSuccessStatusCode();

            var savedUser = await DatabaseHelpers.ReadUser("User2");

            Assert.Equal("__ALTERNATIVE_REG__", savedUser.AlternativeRegistrationNumber);
            Assert.Equal(12.3m, savedUser.CommuteDistance);
            Assert.Equal("__FIRST_NAME__", savedUser.FirstName);
            Assert.Equal("__LAST_NAME__", savedUser.LastName);
            Assert.Equal("__REG__", savedUser.RegistrationNumber);
        }

        private static void CheckUser(
            UsersData actual,
            string expectedUserId,
            string expectedAlternativeRegistrationNumber,
            decimal? expectedCommuteDistance,
            string expectedFirstName,
            string expectedLastName,
            string expectedRegistrationNumber)
        {
            Assert.Equal(expectedUserId, actual.UserId);
            Assert.Equal(expectedAlternativeRegistrationNumber, actual.AlternativeRegistrationNumber);
            Assert.Equal(expectedCommuteDistance, actual.CommuteDistance);
            Assert.Equal(expectedFirstName, actual.FirstName);
            Assert.Equal(expectedLastName, actual.LastName);
            Assert.Equal(expectedRegistrationNumber, actual.RegistrationNumber);
        }
    }
}