namespace Parking.Api.IntegrationTests
{
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Json.Profiles;
    using Microsoft.AspNetCore.Mvc.Testing;
    using TestHelpers;
    using Xunit;
    using static HttpClientHelpers;

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

            var responseContent = await response.Content.ReadAsStringAsync();

            var multipleUsersResponse = JsonHelpers.Deserialize<ProfileResponse>(responseContent);

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

            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            var response = await client.PatchAsync("/profiles", content);

            response.EnsureSuccessStatusCode();

            var savedUser = await DatabaseHelpers.ReadUser("User1");

            Assert.Equal("__ALTERNATIVE_REG__", savedUser.AlternativeRegistrationNumber);
            Assert.Equal("__REG__", savedUser.RegistrationNumber);
        }
    }
}