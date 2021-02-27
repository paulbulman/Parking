// ReSharper disable StringLiteralTypo
// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local
namespace Parking.Api.UnitTests.Controllers
{
    using System.Linq;
    using System.Threading.Tasks;
    using Api.Controllers;
    using Api.Json.Users;
    using Microsoft.AspNetCore.Mvc;
    using TestHelpers;
    using Xunit;
    using static ControllerHelpers;

    public static class UsersControllerTests
    {
        [Fact]
        public static async Task Returns_sorted_list_of_users()
        {
            var users = new[]
            {
                CreateUser.With(
                    userId: "User1",
                    alternativeRegistrationNumber: "BBB",
                    commuteDistance: 12.3m,
                    firstName: "John",
                    lastName: "Doe",
                    registrationNumber: "AAA"),
                CreateUser.With(
                    userId: "User2",
                    alternativeRegistrationNumber: null,
                    commuteDistance: null,
                    firstName: "Laney",
                    lastName: "Asker",
                    registrationNumber: "CCC"),
            };

            var userRepository = CreateUserRepository.WithUsers(users);

            var controller = new UsersController(userRepository);

            var result = await controller.GetAsync();

            var resultValue = GetResultValue<MultipleUsersResponse>(result);

            Assert.NotNull(resultValue.Users);

            var actualUsers = resultValue.Users.ToArray();

            Assert.Equal(2, actualUsers.Length);

            CheckResult(actualUsers[0], "User2", null, null, "Laney", "Asker", "CCC");
            CheckResult(actualUsers[1], "User1", "BBB", 12.3m, "John", "Doe", "AAA");

        }

        [Fact]
        public static async Task Returns_404_response_when_given_user_does_not_exist()
        {
            const string UserId = "User1";

            var userRepository = CreateUserRepository.WithUser(UserId, null);

            var controller = new UsersController(userRepository);

            var result = await controller.GetAsync(UserId);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public static async Task Returns_user_with_the_given_ID()
        {
            const string UserId = "User1";

            var user = CreateUser.With(
                userId: UserId,
                alternativeRegistrationNumber: "X123ABC",
                commuteDistance: 12.3m,
                firstName: "John",
                lastName: "Doe",
                registrationNumber: "AB12XYZ");

            var userRepository = CreateUserRepository.WithUser(UserId, user);

            var controller = new UsersController(userRepository);

            var result = await controller.GetAsync(UserId);

            var resultValue = GetResultValue<SingleUserResponse>(result);

            Assert.NotNull(resultValue.User);

            CheckResult(resultValue.User, UserId, "X123ABC", 12.3m, "John", "Doe", "AB12XYZ");
        }

        private static void CheckResult(
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