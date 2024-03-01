// ReSharper disable StringLiteralTypo
// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local
namespace Parking.Api.UnitTests.Controllers;

using System.Linq;
using System.Threading.Tasks;
using Api.Controllers;
using Api.Json.RegistrationNumbers;
using TestHelpers;
using Xunit;
using static ControllerHelpers;

public static class RegistrationNumbersControllerTests
{
    [Fact]
    public static async Task Returns_empty_list_when_search_string_is_empty()
    {
        var users = new[]
        {
            CreateUser.With(
                userId: "User1",
                firstName: "Mariam",
                lastName: "Brayn",
                alternativeRegistrationNumber: "A12XYZ")
        };

        var controller = new RegistrationNumbersController(CreateUserRepository.WithUsers(users));

        var result = await controller.GetAsync(string.Empty);

        var resultValue = GetResultValue<RegistrationNumbersResponse>(result);

        Assert.NotNull(resultValue.RegistrationNumbers);
        Assert.Empty(resultValue.RegistrationNumbers);
    }

    [Fact]
    public static async Task Filters_registration_numbers_using_search_string()
    {
        var users = new[]
        {
            CreateUser.With(
                userId: "User1",
                firstName: "Mariam",
                lastName: "Brayn",
                alternativeRegistrationNumber: "A12XYZ"),
            CreateUser.With(
                userId: "User2",
                firstName: "Meris",
                lastName: "Wigsell",
                registrationNumber: "Z999ABC")
        };

        var controller = new RegistrationNumbersController(CreateUserRepository.WithUsers(users));

        var result = await controller.GetAsync("A12XYZ");

        var resultValue = GetResultValue<RegistrationNumbersResponse>(result);

        Assert.NotNull(resultValue.RegistrationNumbers);

        var actualRegistrationNumbers = resultValue.RegistrationNumbers.ToArray();

        Assert.Single(actualRegistrationNumbers);

        CheckResult(actualRegistrationNumbers[0], "A12XYZ", "Mariam Brayn");
    }

    [Theory]
    [InlineData("A105XYZ", "a105xyz", "A105XYZ")]
    [InlineData("A105XYZ", "aiosxyz", "A105XYZ")]
    [InlineData("A105XYZ", "aIOSxyz", "A105XYZ")]
    [InlineData("A123XYZ", "a1z3xyz", "A123XYZ")]
    [InlineData("A123XYZ", "A1Z3XYZ", "A123XYZ")]
    [InlineData("A105XYZ", "A105XYZ!\"£$%^&*()-_=+[]{};'#:@~\\|/? ", "A105XYZ")]
    [InlineData("a105xyz", "A105XYZ", "A105XYZ")]
    [InlineData("aiosxyz", "A105XYZ", "AIOSXYZ")]
    [InlineData("aIOSxyz", "A105XYZ", "AIOSXYZ")]
    [InlineData("a1z3xyz", "A123XYZ", "A1Z3XYZ")]
    [InlineData("A1Z3XYZ", "A123XYZ", "A1Z3XYZ")]
    [InlineData("A105XYZ!\"£$%^&*()-_=+[]{};'#:@~\\|/? ", "A105XYZ", "A105XYZ")]
    public static async Task Normalizes_registration_numbers(string savedRegistrationNumber, string searchTerm, string expectedResult)
    {
        var users = new[]
        {
            CreateUser.With(
                userId: "User1",
                firstName: "Mariam",
                lastName: "Brayn",
                registrationNumber: savedRegistrationNumber)
        };

        var controller = new RegistrationNumbersController(CreateUserRepository.WithUsers(users));

        var result = await controller.GetAsync(searchTerm);

        var resultValue = GetResultValue<RegistrationNumbersResponse>(result);

        Assert.NotNull(resultValue.RegistrationNumbers);

        var actualRegistrationNumbers = resultValue.RegistrationNumbers.ToArray();

        Assert.Single(actualRegistrationNumbers);

        CheckResult(actualRegistrationNumbers[0], expectedResult, "Mariam Brayn");
    }

    private static void CheckResult(
        RegistrationNumbersData actual,
        string expectedRegistrationNumber,
        string expectedName)
    {
        Assert.Equal(expectedRegistrationNumber, actual.RegistrationNumber);
        Assert.Equal(expectedName, actual.Name);

    }
}