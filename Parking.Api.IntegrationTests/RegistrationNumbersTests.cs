// ReSharper disable StringLiteralTypo
namespace Parking.Api.IntegrationTests;

using System.Linq;
using System.Threading.Tasks;
using Json.RegistrationNumbers;
using TestHelpers;
using TestHelpers.Aws;
using Xunit;
using static Helpers.HttpClientHelpers;

[Collection("Database tests")]
public class RegistrationNumbersTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory<Startup> factory;

    public RegistrationNumbersTests(CustomWebApplicationFactory<Startup> factory) => this.factory = factory;

    public async Task InitializeAsync() => await DatabaseHelpers.ResetDatabase();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Returns_existing_registration_numbers()
    {
        await DatabaseHelpers.CreateUser(
            CreateUser.With(
                userId: "User1",
                alternativeRegistrationNumber: "X123XYZ",
                firstName: "Kent",
                lastName: "Attewell",
                registrationNumber: "AB12ABC"));
        await DatabaseHelpers.CreateUser(
            CreateUser.With(
                userId: "User2",
                firstName: "Dwayne",
                lastName: "Wanjek",
                registrationNumber: "CD34CDE"));

        var client = this.factory.CreateClient();

        AddAuthorizationHeader(client, UserType.Normal);

        var response = await client.GetAsync("/registrationNumbers/CD34CDE");

        response.EnsureSuccessStatusCode();

        var registrationNumbersResponse = await response.DeserializeAsType<RegistrationNumbersResponse>();

        var actualRegistrationNumbers = registrationNumbersResponse.RegistrationNumbers.ToArray();

        Assert.Single(actualRegistrationNumbers);

        Assert.Equal("Dwayne Wanjek", actualRegistrationNumbers[0].Name);
        Assert.Equal("CD34CDE", actualRegistrationNumbers[0].RegistrationNumber);
    }
}