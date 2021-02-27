// ReSharper disable StringLiteralTypo
namespace Parking.Api.UnitTests.Controllers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Api.Controllers;
    using Api.Json.Reservations;
    using Model;
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
        public static async Task Returns_saved_reservations()
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

            Assert.Equal(new[] {"User1", "User2", "User3"}, data.UserIds);
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
    }
}