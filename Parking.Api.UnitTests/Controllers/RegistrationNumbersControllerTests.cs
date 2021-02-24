// ReSharper disable StringLiteralTypo
// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local
namespace Parking.Api.UnitTests.Controllers
{
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
        public static async Task Returns_formatted_registration_number_and_name()
        {
            var users = new[]
            {
                CreateUser.With(
                    userId: "User1",
                    firstName: "Mariam",
                    lastName: "Brayn",
                    registrationNumber: "a 12 xyz",
                    alternativeRegistrationNumber: "z 999 abc")
            };

            var controller = new RegistrationNumbersController(CreateUserRepository.WithUsers(users));

            var result = await controller.GetAsync();

            var response = GetResultValue<RegistrationNumbersResponse>(result);

            Assert.NotNull(response.RegistrationNumbers);

            var actualRegistrationNumbers = response.RegistrationNumbers.ToArray();

            Assert.Equal(2, actualRegistrationNumbers.Length);

            Assert.All(actualRegistrationNumbers, r => Assert.Equal("Mariam Brayn", r.Name));

            CheckData(actualRegistrationNumbers[0], "A12XYZ", "Mariam Brayn");
            CheckData(actualRegistrationNumbers[1], "Z999ABC", "Mariam Brayn");
        }

        [Fact]
        public static async Task Filters_blank_registration_numbers()
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

            var result = await controller.GetAsync();

            var response = GetResultValue<RegistrationNumbersResponse>(result);

            Assert.NotNull(response.RegistrationNumbers);

            var actualRegistrationNumbers = response.RegistrationNumbers.ToArray();

            Assert.Equal(2, actualRegistrationNumbers.Length);

            CheckData(actualRegistrationNumbers[0], "A12XYZ", "Mariam Brayn");
            CheckData(actualRegistrationNumbers[1], "Z999ABC", "Meris Wigsell");
        }

        [Fact]
        public static async Task Returns_data_sorted_by_registration_number()
        {
            var users = new[]
            {
                CreateUser.With(
                    userId: "User1",
                    firstName: "Mariam",
                    lastName: "Brayn",
                    registrationNumber: "BBB",
                    alternativeRegistrationNumber: "ccc"),
                CreateUser.With(
                    userId: "User2",
                    firstName: "Meris",
                    lastName: "Wigsell",
                    registrationNumber: "aaa",
                    alternativeRegistrationNumber: "DDD")
            };

            var controller = new RegistrationNumbersController(CreateUserRepository.WithUsers(users));

            var result = await controller.GetAsync();

            var response = GetResultValue<RegistrationNumbersResponse>(result);

            Assert.NotNull(response.RegistrationNumbers);

            var actualRegistrationNumbers = response.RegistrationNumbers.ToArray();

            Assert.Equal(4, actualRegistrationNumbers.Length);

            CheckData(actualRegistrationNumbers[0], "AAA", "Meris Wigsell"); 
            CheckData(actualRegistrationNumbers[1], "BBB", "Mariam Brayn");
            CheckData(actualRegistrationNumbers[2], "CCC", "Mariam Brayn");
            CheckData(actualRegistrationNumbers[3], "DDD", "Meris Wigsell");
        }

        private static void CheckData(
            RegistrationNumbersData actual,
            string expectedRegistrationNumber,
            string expectedName)
        {
            Assert.Equal(expectedRegistrationNumber, actual.RegistrationNumber);
            Assert.Equal(expectedName, actual.Name);

        }
    }
}