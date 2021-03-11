namespace Parking.Api.IntegrationTests
{
    using System.Threading.Tasks;
    using Helpers;
    using Json.Profiles;
    using Microsoft.AspNetCore.Mvc.Testing;
    using TestHelpers;
    using Xunit;
    using static Helpers.HttpClientHelpers;

    [Collection("Database tests")]
    public class ProfilesTests : IAsyncLifetime
    {
        private readonly WebApplicationFactory<Startup> factory;

        public ProfilesTests(CustomWebApplicationFactory<Startup> factory) => this.factory = factory;

        public async Task InitializeAsync() => await DatabaseHelpers.ResetDatabase();

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task Returns_existing_profile()
        {
            await DatabaseHelpers.CreateUser(
                CreateUser.With(
                    userId: "User1", alternativeRegistrationNumber: "X123XYZ", registrationNumber: "AB12ABC"));

            var client = this.factory.CreateClient();

            AddAuthorizationHeader(client, UserType.Normal);

            var response = await client.GetAsync("/profiles");

            response.EnsureSuccessStatusCode();

            var multipleUsersResponse = await response.DeserializeAsType<ProfileResponse>();

            var actualProfile = multipleUsersResponse.Profile;

            Assert.Equal("X123XYZ", actualProfile.AlternativeRegistrationNumber);
            Assert.Equal("AB12ABC", actualProfile.RegistrationNumber);
        }

        [Fact]
        public async Task Updates_existing_profile()
        {
            await DatabaseHelpers.CreateUser(
                CreateUser.With(
                    userId: "User1",
                    alternativeRegistrationNumber: "X123XYZ",
                    commuteDistance: 12.3m,
                    registrationNumber: "AB12ABC"));

            var client = this.factory.CreateClient();

            AddAuthorizationHeader(client, UserType.Normal);

            var request = new ProfilePatchRequest("__ALTERNATIVE_REG__", "__REG__");

            var response = await client.PatchAsJsonAsync("/profiles", request);

            response.EnsureSuccessStatusCode();

            var savedUser = await DatabaseHelpers.ReadUser("User1");

            Assert.Equal("__ALTERNATIVE_REG__", savedUser.AlternativeRegistrationNumber);
            Assert.Equal("__REG__", savedUser.RegistrationNumber);
        }
    }
}