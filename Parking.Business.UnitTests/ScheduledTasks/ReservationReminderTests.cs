namespace Parking.Business.UnitTests.ScheduledTasks
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Business.ScheduledTasks;
    using Data;
    using Model;
    using Moq;
    using NodaTime.Testing.Extensions;
    using TestHelpers;
    using Xunit;
    using IEmailTemplate = Business.EmailTemplates.IEmailTemplate;
    using static DateCalculatorTests;

    public static class ReservationReminderTests
    {
        [Fact]
        public static async Task Sends_emails_to_team_leaders_when_no_reservations_have_been_entered()
        {
            var nextWorkingDate = 23.December(2020);
            
            var mockDateCalculator = new Mock<IDateCalculator>(MockBehavior.Strict);
            mockDateCalculator
                .Setup(c => c.GetNextWorkingDate())
                .Returns(nextWorkingDate);

            var mockEmailRepository = new Mock<IEmailRepository>();

            var mockReservationRepository = new Mock<IReservationRepository>(MockBehavior.Strict);
            mockReservationRepository
                .Setup(r => r.GetReservations(nextWorkingDate, nextWorkingDate))
                .ReturnsAsync(new List<Reservation>());
            
            var teamLeaderUsers = new[]
            {
                CreateUser.With(userId: "user1", emailAddress: "1@abc.com"),
                CreateUser.With(userId: "user2", emailAddress: "2@xyz.co.uk"),
            };

            var mockUserRepository = new Mock<IUserRepository>(MockBehavior.Strict);
            mockUserRepository
                .Setup(r => r.GetTeamLeaderUsers())
                .ReturnsAsync(teamLeaderUsers);

            var reservationReminder = new ReservationReminder(
                mockDateCalculator.Object,
                mockEmailRepository.Object,
                mockReservationRepository.Object,
                mockUserRepository.Object);
            
            await reservationReminder.Run();

            mockEmailRepository.Verify(
                r => r.Send(It.Is<IEmailTemplate>(e =>
                    e.To == "1@abc.com" && e.Subject == "No parking reservations entered for Wed 23 Dec")),
                Times.Once());
            mockEmailRepository.Verify(
                r => r.Send(It.Is<IEmailTemplate>(e =>
                    e.To == "2@xyz.co.uk" && e.Subject == "No parking reservations entered for Wed 23 Dec")),
                Times.Once());
            mockEmailRepository.VerifyNoOtherCalls();
        }

        [Fact]
        public static async Task Does_not_send_email_when_reservations_have_been_entered()
        {
            var nextWorkingDate = 23.December(2020);

            var mockDateCalculator = new Mock<IDateCalculator>(MockBehavior.Strict);
            mockDateCalculator
                .Setup(c => c.GetNextWorkingDate())
                .Returns(nextWorkingDate);

            var mockEmailRepository = new Mock<IEmailRepository>();

            var reservations = new[] {new Reservation("user1", nextWorkingDate)};
            
            var mockReservationRepository = new Mock<IReservationRepository>(MockBehavior.Strict);
            mockReservationRepository
                .Setup(r => r.GetReservations(nextWorkingDate, nextWorkingDate))
                .ReturnsAsync(reservations);

            var reservationReminder = new ReservationReminder(
                mockDateCalculator.Object,
                mockEmailRepository.Object,
                mockReservationRepository.Object,
                Mock.Of<IUserRepository>(MockBehavior.Strict));

            await reservationReminder.Run();

            mockEmailRepository.VerifyNoOtherCalls();
        }

        [Fact]
        public static void ScheduledTaskType_returns_ReservationReminder()
        {
            var reservationReminder = new ReservationReminder(
                Mock.Of<IDateCalculator>(),
                Mock.Of<IEmailRepository>(),
                Mock.Of<IReservationRepository>(),
                Mock.Of<IUserRepository>());
            
            Assert.Equal(ScheduledTaskType.ReservationReminder, reservationReminder.ScheduledTaskType);
        }

        [Theory]
        [InlineData(21, 22)]
        [InlineData(24, 29)]
        public static void GetNextRunTime_returns_10_am_on_next_working_day(int currentDay, int expectedNextDay)
        {
            var bankHolidays = new[] { 25.December(2020), 28.December(2020) };

            var dateCalculator = CreateDateCalculator(currentDay.December(2020).At(10, 0, 0).Utc(), bankHolidays);

            var actual = new ReservationReminder(
                dateCalculator,
                Mock.Of<IEmailRepository>(),
                Mock.Of<IReservationRepository>(),
                Mock.Of<IUserRepository>()).GetNextRunTime();

            var expected = expectedNextDay.December(2020).At(10, 0, 0).Utc();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void GetNextRunTime_uses_London_time_zone()
        {
            var dateCalculator = CreateDateCalculator(27.March(2020).At(10, 0, 0).Utc());

            var actual = new ReservationReminder(
                dateCalculator,
                Mock.Of<IEmailRepository>(),
                Mock.Of<IReservationRepository>(),
                Mock.Of<IUserRepository>()).GetNextRunTime();

            var expected = 30.March(2020).At(9, 0, 0).Utc();

            Assert.Equal(expected, actual);
        }
    }
}