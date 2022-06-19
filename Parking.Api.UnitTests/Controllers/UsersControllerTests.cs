﻿// ReSharper disable StringLiteralTypo
// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local
namespace Parking.Api.UnitTests.Controllers
{
    using System.Linq;
    using System.Threading.Tasks;
    using Api.Controllers;
    using Api.Json.Users;
    using Business;
    using Business.Data;
    using Microsoft.AspNetCore.Mvc;
    using Model;
    using Moq;
    using NodaTime;
    using NodaTime.Testing.Extensions;
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

            var controller = CreateController(userRepository);

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

            var controller = CreateController(userRepository);

            var result = await controller.GetByIdAsync(UserId);

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

            var controller = CreateController(userRepository);

            var result = await controller.GetByIdAsync(UserId);

            var resultValue = GetResultValue<SingleUserResponse>(result);

            Assert.NotNull(resultValue.User);

            CheckResult(resultValue.User, UserId, "X123ABC", 12.3m, "John", "Doe", "AB12XYZ");
        }

        [Fact]
        public static async Task Creates_new_user()
        {
            var mockUserRepository = new Mock<IUserRepository>();
            mockUserRepository
                .Setup(r => r.CreateUser(It.IsAny<User>()))
                .ReturnsAsync(CreateUser.With(userId: "User1"));

            var controller = CreateController(mockUserRepository.Object);

            var request = new UserPostRequest("Z999ABC", 12.3M, "john.doe@example.com", "John", "Doe", "AB12CDE");

            await controller.PostAsync(request);

            mockUserRepository.Verify(r => r.CreateUser(It.Is<User>(u =>
                u.AlternativeRegistrationNumber == "Z999ABC" &&
                u.CommuteDistance == 12.3m &&
                u.EmailAddress == "john.doe@example.com" &&
                u.FirstName == "John" &&
                u.LastName == "Doe" &&
                u.RegistrationNumber == "AB12CDE" &&
                u.RequestReminderEnabled == true &&
                u.ReservationReminderEnabled == true)));

            mockUserRepository.VerifyNoOtherCalls();
        }

        [Fact]
        public static async Task Returns_newly_created_user()
        {
            var returnedUser = CreateUser.With(
                userId: "User1",
                alternativeRegistrationNumber: "__ALTERNATIVE_REG__",
                commuteDistance: 99,
                emailAddress: "__EMAIL_ADDRESS__",
                firstName: "__FIRST_NAME__",
                lastName: "__LAST_NAME__",
                registrationNumber: "__REG__");

            var mockUserRepository = new Mock<IUserRepository>();
            mockUserRepository
                .Setup(r => r.CreateUser(It.IsAny<User>()))
                .ReturnsAsync(returnedUser);

            var controller = CreateController(mockUserRepository.Object);

            var request = new UserPostRequest("Z999ABC", 12.3M, "john.doe@example.com", "John", "Doe", "AB12CDE");

            var result = await controller.PostAsync(request);

            var resultValue = GetResultValue<SingleUserResponse>(result);

            Assert.NotNull(resultValue.User);

            CheckResult(
                resultValue.User, "User1", "__ALTERNATIVE_REG__", 99, "__FIRST_NAME__", "__LAST_NAME__", "__REG__");
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
                registrationNumber: "AB12CDE",
                requestReminderEnabled: true,
                reservationReminderEnabled: false);

            var mockUserRepository = new Mock<IUserRepository>();

            mockUserRepository
                .Setup(r => r.GetUser(UserId))
                .ReturnsAsync(existingUser);

            var request = new UserPatchRequest(
                "__NEW_ALTERNATIVE_REG__", 99.9m, "__NEW_FIRST_NAME__", "__NEW_LAST_NAME__", "__NEW_REG__");

            var controller = CreateController(mockUserRepository.Object);

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
                        u.RegistrationNumber == "__NEW_REG__" &&
                        u.RequestReminderEnabled == true &&
                        u.ReservationReminderEnabled == false)),
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

            var controller = CreateController(mockUserRepository.Object);

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

            var controller = CreateController(userRepository);

            var result = await controller.PatchAsync(UserId, request);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public static async Task Deletes_user_and_related_entities()
        {
            const string UserId = "User1";

            var activeDates = new[] { 28.June(2021), 29.June(2021), 1.July(2021) };

            var mockDateCalculator = new Mock<IDateCalculator>(MockBehavior.Strict);
            mockDateCalculator
                .Setup(d => d.GetActiveDates())
                .Returns(activeDates);
            mockDateCalculator
                .Setup(d => d.GetCalculationWindow())
                .Returns(new DateInterval(1.June(2021), 29.June(2021)));

            var existingUser = CreateUser.With(userId: UserId);

            var mockUserRepository = new Mock<IUserRepository>();
            mockUserRepository
                .Setup(r => r.GetUser(UserId))
                .ReturnsAsync(existingUser);

            var mockRequestRepository = new Mock<IRequestRepository>();

            var mockReservationRepository = new Mock<IReservationRepository>();

            var controller = new UsersController(
                mockDateCalculator.Object,
                mockRequestRepository.Object,
                mockReservationRepository.Object,
                mockUserRepository.Object);

            await controller.DeleteAsync(UserId);

            mockRequestRepository.Verify(
                r => r.DeleteRequests(existingUser, new DateInterval(1.June(2021), 1.July(2021))),
                Times.Once);

            mockReservationRepository.Verify(
                r => r.DeleteReservations(existingUser, new DateInterval(1.June(2021), 1.July(2021))),
                Times.Once);

            mockUserRepository.Verify(r => r.DeleteUser(It.IsAny<User>()), Times.Once);
        }

        [Fact]
        public static async Task Returns_404_response_when_given_user_to_delete_does_not_exist()
        {
            const string UserId = "User1";

            var userRepository = CreateUserRepository.WithUser(UserId, null);

            var controller = CreateController(userRepository);

            var result = await controller.DeleteAsync(UserId);

            Assert.IsType<NotFoundResult>(result);
        }

        private static UsersController CreateController(IUserRepository userRepository) => 
            new UsersController(
                Mock.Of<IDateCalculator>(),
                Mock.Of<IRequestRepository>(),
                Mock.Of<IReservationRepository>(),
                userRepository);

        private static void CheckResult(
            UsersData actual,
            string expectedUserId,
            string? expectedAlternativeRegistrationNumber,
            decimal? expectedCommuteDistance,
            string expectedFirstName,
            string expectedLastName,
            string? expectedRegistrationNumber)
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