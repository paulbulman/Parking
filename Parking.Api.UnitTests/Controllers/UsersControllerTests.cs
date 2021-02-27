// ReSharper disable StringLiteralTypo
// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local
namespace Parking.Api.UnitTests.Controllers
{
    using System.Linq;
    using System.Threading.Tasks;
    using Api.Controllers;
    using Api.Json.Users;
    using Business.Data;
    using Microsoft.AspNetCore.Mvc;
    using Model;
    using Moq;
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
        public static async Task Returns_404_response_when_given_user_to_fetch_does_not_exist()
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

        [Fact]
        public static async Task Saves_combined_updated_editable_properties_and_existing_readonly_properties()
        {
            const string UserId = "User1";

            var existingUser = CreateUser.With(
                userId: UserId,
                alternativeRegistrationNumber: "Z999ABC",
                commuteDistance: 12.3m,
                emailAddress: "john.doe@example.com",
                firstName: "John",
                lastName: "Doe",
                registrationNumber: "AB12CDE");

            var mockUserRepository = new Mock<IUserRepository>();

            mockUserRepository
                .Setup(r => r.GetUser(UserId))
                .ReturnsAsync(existingUser);

            var request = new UserPatchRequest(
                "__NEW_ALTERNATIVE_REG__", 99.9m, "__NEW_FIRST_NAME__", "__NEW_LAST_NAME__", "__NEW_REG__");

            var controller = new UsersController(mockUserRepository.Object);

            await controller.PatchAsync(UserId, request);

            mockUserRepository.Verify(r => r.GetUser(UserId), Times.Once);

            mockUserRepository.Verify(
                r => r.SaveUser(
                    It.Is<User>(u =>
                        u.UserId == UserId &&
                        u.AlternativeRegistrationNumber == "__NEW_ALTERNATIVE_REG__" &&
                        u.CommuteDistance == 99.9m &&
                        u.EmailAddress == "john.doe@example.com" &&
                        u.FirstName == "__NEW_FIRST_NAME__" &&
                        u.LastName == "__NEW_LAST_NAME__" &&
                        u.RegistrationNumber == "__NEW_REG__")),
                Times.Once);

            mockUserRepository.VerifyNoOtherCalls();
        }

        [Fact]
        public static async Task Returns_updated_user_after_updating()
        {
            const string UserId = "User1";

            var existingUser = CreateUser.With(userId: UserId);

            var mockUserRepository = new Mock<IUserRepository>();

            mockUserRepository
                .Setup(r => r.GetUser(UserId))
                .ReturnsAsync(existingUser);

            var request = new UserPatchRequest(
                "__NEW_ALTERNATIVE_REG__", 99.9m, "__NEW_FIRST_NAME__", "__NEW_LAST_NAME__", "__NEW_REG__");

            var controller = new UsersController(mockUserRepository.Object);

            var result = await controller.PatchAsync(UserId, request);

            var resultValue = GetResultValue<SingleUserResponse>(result);

            Assert.NotNull(resultValue.User);

            CheckResult(
                resultValue.User,
                UserId,
                "__NEW_ALTERNATIVE_REG__",
                99.9m,
                "__NEW_FIRST_NAME__",
                "__NEW_LAST_NAME__", 
                "__NEW_REG__");
        }

        [Fact]
        public static async Task Returns_404_response_when_given_user_to_update_does_not_exist()
        {
            const string UserId = "User1";

            var userRepository = CreateUserRepository.WithUser(UserId, null);

            var request = new UserPatchRequest(
                "__NEW_ALTERNATIVE_REG__", 99.9m, "__NEW_FIRST_NAME__", "__NEW_LAST_NAME__", "__NEW_REG__");

            var controller = new UsersController(userRepository);

            var result = await controller.PatchAsync(UserId, request);

            Assert.IsType<NotFoundResult>(result);
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