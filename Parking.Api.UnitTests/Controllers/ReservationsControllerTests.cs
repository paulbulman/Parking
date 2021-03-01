// ReSharper disable StringLiteralTypo
namespace Parking.Api.UnitTests.Controllers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Api.Controllers;
    using Api.Json.Reservations;
    using Business.Data;
    using Model;
    using Moq;
    using NodaTime.Testing.Extensions;
    using TestHelpers;
    using Xunit;
    using static ControllerHelpers;
    using static Json.Calendar.CalendarHelpers;

    public static class ReservationsControllerTests
    {
        [Fact]
        public static async Task Returns_reservations_data_for_each_active_date()
        {
            var activeDates = new[] { 15.February(2021), 16.February(2021), 18.February(2021) };

            var users = new[] { CreateUser.With(userId: "User1") };

            var controller = new ReservationsController(
                CreateConfigurationRepository.WithDefaultConfiguration(),
                CreateDateCalculator.WithActiveDates(activeDates),
                CreateReservationRepository.WithReservations(activeDates, new List<Reservation>()),
                CreateUserRepository.WithUsers(users));

            var result = await controller.GetAsync();

            var resultValue = GetResultValue<ReservationsResponse>(result);

            Assert.NotNull(resultValue.Reservations);

            var visibleDays = GetVisibleDays(resultValue.Reservations);

            Assert.Equal(activeDates, visibleDays.Select(d => d.LocalDate));

            Assert.All(visibleDays, d => Assert.NotNull(d.Data));
        }

        [Fact]
        public static async Task Returns_previously_saved_reservations()
        {
            var activeDates = new[] { 15.February(2021), 16.February(2021), 18.February(2021) };

            var reservations = new[]
            {
                new Reservation("User1", 15.February(2021)),
                new Reservation("User2", 15.February(2021)),
                new Reservation("User3", 15.February(2021)),
            };

            var users = new[] { CreateUser.With(userId: "User1") };

            var controller = new ReservationsController(
                CreateConfigurationRepository.WithDefaultConfiguration(),
                CreateDateCalculator.WithActiveDates(activeDates),
                CreateReservationRepository.WithReservations(activeDates, reservations),
                CreateUserRepository.WithUsers(users));

            var result = await controller.GetAsync();

            var resultValue = GetResultValue<ReservationsResponse>(result);

            var data = GetDailyData(resultValue.Reservations, 15.February(2021));

            Assert.Equal(new[] { "User1", "User2", "User3" }, data.UserIds);
        }

        [Fact]
        public static async Task Returns_short_lead_time_spaces_count()
        {
            var configuration = CreateConfiguration.With(shortLeadTimeSpaces: 2);

            var activeDates = new[] { 15.February(2021) };

            var controller = new ReservationsController(
                CreateConfigurationRepository.WithConfiguration(configuration),
                CreateDateCalculator.WithActiveDates(activeDates),
                CreateReservationRepository.WithReservations(activeDates, new List<Reservation>()),
                CreateUserRepository.WithUsers(new List<User>()));

            var result = await controller.GetAsync();

            var resultValue = GetResultValue<ReservationsResponse>(result);

            Assert.Equal(2, resultValue.ShortLeadTimeSpaces);
        }

        [Fact]
        public static async Task Returns_sorted_list_of_users()
        {
            var activeDates = new[] { 15.February(2021) };

            var users = new[]
            {
                CreateUser.With(userId: "User1", firstName: "Silvester", lastName: "Probet"),
                CreateUser.With(userId: "User2", firstName: "Kendricks", lastName: "Hawke"),
                CreateUser.With(userId: "User3", firstName: "Rupert", lastName: "Trollope"),
            };

            var controller = new ReservationsController(
                CreateConfigurationRepository.WithDefaultConfiguration(),
                CreateDateCalculator.WithActiveDates(activeDates),
                CreateReservationRepository.WithReservations(activeDates, new List<Reservation>()),
                CreateUserRepository.WithUsers(users));

            var result = await controller.GetAsync();

            var resultValue = GetResultValue<ReservationsResponse>(result);

            Assert.NotNull(resultValue.Users);

            var actualUsers = resultValue.Users.ToArray();

            Assert.Equal(3, actualUsers.Length);

            Assert.Equal("User2", actualUsers[0].UserId);
            Assert.Equal("Kendricks Hawke", actualUsers[0].Name);

            Assert.Equal("User1", actualUsers[1].UserId);
            Assert.Equal("Silvester Probet", actualUsers[1].Name);

            Assert.Equal("User3", actualUsers[2].UserId);
            Assert.Equal("Rupert Trollope", actualUsers[2].Name);
        }

        [Fact]
        public static async Task Returns_empty_reservations_object_when_no_reservations_exist()
        {
            var activeDates = new[] { 15.February(2021) };

            var controller = new ReservationsController(
                CreateConfigurationRepository.WithDefaultConfiguration(),
                CreateDateCalculator.WithActiveDates(activeDates),
                CreateReservationRepository.WithReservations(activeDates, new List<Reservation>()),
                CreateUserRepository.WithUsers(new List<User>()));

            var result = await controller.GetAsync();

            var resultValue = GetResultValue<ReservationsResponse>(result);

            var data = GetDailyData(resultValue.Reservations, 15.February(2021));

            Assert.NotNull(data.UserIds);

            Assert.Empty(data.UserIds);
        }

        [Fact]
        public static async Task Saves_reservations()
        {
            var activeDates = new[] { 2.February(2021), 3.February(2021) };

            var mockReservationRepository =
                CreateReservationRepository.MockWithReservations(activeDates, new List<Reservation>());

            mockReservationRepository
                .Setup(r => r.SaveReservations(It.IsAny<IReadOnlyCollection<Reservation>>()))
                .Returns(Task.CompletedTask);

            var patchRequest = new ReservationsPatchRequest(new[]
            {
                new ReservationsPatchRequestDailyData(2.February(2021),
                    new ReservationsData(new List<string> {"User1", "User2"})),
                new ReservationsPatchRequestDailyData(3.February(2021),
                    new ReservationsData(new List<string> {"User2", "User3"})),
            });

            var controller = new ReservationsController(
                CreateConfigurationRepository.WithDefaultConfiguration(),
                CreateDateCalculator.WithActiveDates(activeDates),
                mockReservationRepository.Object,
                CreateUserRepository.WithUsers(new List<User>()));

            await controller.PatchAsync(patchRequest);

            var expectedSavedReservations = new[]
            {
                new Reservation("User1", 2.February(2021)),
                new Reservation("User2", 2.February(2021)),
                new Reservation("User2", 3.February(2021)),
                new Reservation("User3", 3.February(2021)),
            };

            CheckSavedReservations(mockReservationRepository, expectedSavedReservations);
        }

        [Fact]
        public static async Task Does_not_save_reservations_outside_active_date_range()
        {
            var activeDates = new[] { 2.February(2021), 3.February(2021) };

            var mockReservationRepository =
                CreateReservationRepository.MockWithReservations(activeDates, new List<Reservation>());

            mockReservationRepository
                .Setup(r => r.SaveReservations(It.IsAny<IReadOnlyCollection<Reservation>>()))
                .Returns(Task.CompletedTask);

            var patchRequest = new ReservationsPatchRequest(new[]
            {
                new ReservationsPatchRequestDailyData(1.February(2021),
                    new ReservationsData(new List<string> {"User1", "User2"})),
                new ReservationsPatchRequestDailyData(4.February(2021),
                    new ReservationsData(new List<string> {"User1", "User2"})),
            });

            var controller = new ReservationsController(
                CreateConfigurationRepository.WithDefaultConfiguration(),
                CreateDateCalculator.WithActiveDates(activeDates),
                mockReservationRepository.Object,
                CreateUserRepository.WithUsers(new List<User>()));

            await controller.PatchAsync(patchRequest);

            CheckSavedReservations(mockReservationRepository, new List<Reservation>());
        }

        [Fact]
        public static async Task Returns_updated_reservations_after_saving()
        {
            var activeDates = new[] { 2.February(2021), 3.February(2021) };

            var returnedReservations = new[]
            {
                new Reservation("User1", 2.February(2021)),
                new Reservation("User2", 2.February(2021)),
                new Reservation("User2", 3.February(2021)),
                new Reservation("User3", 3.February(2021)),
            };

            var mockReservationRepository =
                CreateReservationRepository.MockWithReservations(activeDates, returnedReservations);

            mockReservationRepository
                .Setup(r => r.SaveReservations(It.IsAny<IReadOnlyCollection<Reservation>>()))
                .Returns(Task.CompletedTask);

            var patchRequest = new ReservationsPatchRequest(new[]
            {
                new ReservationsPatchRequestDailyData(
                    2.February(2021),
                    new ReservationsData(new[] {"User1", "User2"}))
            });

            var controller = new ReservationsController(
                CreateConfigurationRepository.WithDefaultConfiguration(),
                CreateDateCalculator.WithActiveDates(activeDates),
                mockReservationRepository.Object,
                CreateUserRepository.WithUsers(new List<User>()));

            var result = await controller.PatchAsync(patchRequest);

            var resultValue = GetResultValue<ReservationsResponse>(result);

            var day1Data = GetDailyData(resultValue.Reservations, 2.February(2021));
            var day2Data = GetDailyData(resultValue.Reservations, 3.February(2021));

            Assert.Equal(new[] { "User1", "User2" }, day1Data.UserIds);
            Assert.Equal(new[] { "User2", "User3" }, day2Data.UserIds);
        }

        private static void CheckSavedReservations(
            Mock<IReservationRepository> mockReservationRepository,
            IReadOnlyCollection<Reservation> expectedSavedReservations)
        {
            mockReservationRepository.Verify(
                r => r.SaveReservations(It.Is<IReadOnlyCollection<Reservation>>(
                    actual => CheckReservations(expectedSavedReservations, actual.ToList()))),
                Times.Once);
        }

        private static bool CheckReservations(
            IReadOnlyCollection<Reservation> expected,
            IReadOnlyCollection<Reservation> actual) =>
            actual.Count == expected.Count && expected.All(e => actual.Contains(e, new ReservationsComparer()));

        private class ReservationsComparer : IEqualityComparer<Reservation>
        {
            public bool Equals(Reservation first, Reservation second) =>
                first != null &&
                second != null &&
                first.UserId == second.UserId &&
                first.Date == second.Date;

            public int GetHashCode(Reservation reservation) => reservation.Date.GetHashCode();
        }
    }
}