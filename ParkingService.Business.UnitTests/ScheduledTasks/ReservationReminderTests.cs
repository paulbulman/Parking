namespace ParkingService.Business.UnitTests.ScheduledTasks
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Business.ScheduledTasks;
    using Data;
    using Model;
    using Moq;
    using NodaTime.Testing.Extensions;
    using Xunit;
    using IEmailTemplate = Business.EmailTemplates.IEmailTemplate;

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
                new User("user1", null, "1@abc.com"),
                new User("user2", null, "2@xyz.co.uk")
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
        public static void Returns_reservation_reminder_for_task_type()
        {
            var reservationReminder = new ReservationReminder(
                Mock.Of<IDateCalculator>(),
                Mock.Of<IEmailRepository>(),
                Mock.Of<IReservationRepository>(),
                Mock.Of<IUserRepository>());
            
            Assert.Equal(ScheduledTaskType.ReservationReminder, reservationReminder.ScheduledTaskType);
        }
    }
}