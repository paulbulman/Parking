namespace Parking.Api.UnitTests.Controllers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Api.Controllers;
    using Api.Json.History;
    using Business;
    using Business.Data;
    using Model;
    using Moq;
    using NodaTime;
    using NodaTime.Testing.Extensions;
    using TestHelpers;
    using Xunit;
    using static ControllerHelpers;
    using static Json.Calendar.CalendarHelpers;

    public static class HistoryControllerTests
    {
        private static readonly LocalDate FirstDate = 2.August(2021);
        private static readonly LocalDate LastDate = 1.October(2021);

        [Fact]
        public static async Task Returns_data_for_previous_two_months()
        {
            var controller = new HistoryController(
                CreateDefaultDateCalculator(),
                CreateDefaultRequestRepository(),
                CreateDefaultReservationRepository());

            var result = await controller.GetAsync("__USER_ID__", LastDate);

            var resultValue = GetResultValue<HistoryResponse>(result);

            var visibleDays = GetVisibleDays(resultValue.History);

            Assert.Equal(FirstDate, visibleDays.First().LocalDate);
            Assert.Equal(LastDate, visibleDays.Last().LocalDate);
        }

        [Fact]
        public static async Task Hides_non_working_days()
        {
            var bankHoliday = 30.August(2021);

            var mockDateCalculator = new Mock<IDateCalculator>();
            mockDateCalculator
                .Setup(d => d.IsWorkingDay(It.IsAny<LocalDate>()))
                .Returns((LocalDate date) => date != bankHoliday);

            var controller = new HistoryController(
                mockDateCalculator.Object,
                CreateDefaultRequestRepository(),
                CreateDefaultReservationRepository());

            var result = await controller.GetAsync("__USER_ID__", LastDate);

            var resultValue = GetResultValue<HistoryResponse>(result);

            var visibleDays = GetVisibleDays(resultValue.History);

            Assert.DoesNotContain(bankHoliday, visibleDays.Select(d => d.LocalDate));
        }

        [Fact]
        public static async Task Returns_caption_based_on_requests()
        {
            var requests = new[]
            {
                new Request("USER1", 20.September(2021), RequestStatus.Allocated),
                
                new Request("USER1", 21.September(2021), RequestStatus.Allocated),
                new Request("USER2", 21.September(2021), RequestStatus.Interrupted),

                new Request("USER1", 22.September(2021), RequestStatus.Allocated),
                new Request("USER2", 22.September(2021), RequestStatus.Interrupted),

                new Request("USER1", 27.September(2021), RequestStatus.Interrupted),
                new Request("USER2", 27.September(2021), RequestStatus.Allocated),

                new Request("USER1", 28.September(2021), RequestStatus.SoftInterrupted),
                new Request("USER2", 28.September(2021), RequestStatus.Allocated),

                new Request("USER1", 29.September(2021), RequestStatus.HardInterrupted),
                new Request("USER2", 29.September(2021), RequestStatus.Allocated),

                new Request("USER1", 30.September(2021), RequestStatus.Cancelled),

                new Request("USER1", 1.October(2021), RequestStatus.Pending),
            };

            var reservations = new[]
            {
                new Reservation("USER1", 22.September(2021)),
            };

            var dateInterval = new DateInterval(FirstDate, LastDate);

            var requestRepository = new RequestRepositoryBuilder()
                .WithGetRequests(dateInterval, requests)
                .Build();
                
            var reservationRepository = new ReservationRepositoryBuilder()
                .WithGetReservations(dateInterval, reservations)
                .Build();

            var controller = new HistoryController(
                CreateDefaultDateCalculator(),
                requestRepository,
                reservationRepository);

            var result = await controller.GetAsync("USER1", LastDate);

            var actual = GetResultValue<HistoryResponse>(result).History;

            Assert.Equal(string.Empty, GetDailyData(actual, 13.September(2021)));

            Assert.Equal("Allocated (uncontested)", GetDailyData(actual, 20.September(2021)));
            Assert.Equal("Allocated (contested)", GetDailyData(actual, 21.September(2021)));
            Assert.Equal("Allocated (reserved)", GetDailyData(actual, 22.September(2021)));
            
            Assert.Equal("Interrupted", GetDailyData(actual, 27.September(2021)));
            Assert.Equal("Interrupted (day ahead)", GetDailyData(actual, 28.September(2021)));
            Assert.Equal("Interrupted (stay interrupted)", GetDailyData(actual, 29.September(2021)));
            
            Assert.Equal("Cancelled", GetDailyData(actual, 30.September(2021)));
            Assert.Equal("Pending", GetDailyData(actual, 1.October(2021)));
        }

        [Fact]
        public static async Task Returns_allocation_counts_based_on_requests()
        {
            var requests = new[]
            {
                new Request("USER1", 20.September(2021), RequestStatus.Allocated),

                new Request("USER1", 21.September(2021), RequestStatus.Allocated),
                new Request("USER2", 21.September(2021), RequestStatus.Interrupted),

                new Request("USER1", 22.September(2021), RequestStatus.Allocated),
                new Request("USER2", 22.September(2021), RequestStatus.Interrupted),

                new Request("USER1", 27.September(2021), RequestStatus.Interrupted),
                new Request("USER2", 27.September(2021), RequestStatus.Allocated),

                new Request("USER1", 28.September(2021), RequestStatus.SoftInterrupted),
                new Request("USER2", 28.September(2021), RequestStatus.Allocated),

                new Request("USER1", 29.September(2021), RequestStatus.HardInterrupted),
                new Request("USER2", 29.September(2021), RequestStatus.Allocated),

                new Request("USER1", 30.September(2021), RequestStatus.Cancelled),

                new Request("USER1", 1.October(2021), RequestStatus.Pending),
            };

            var reservations = new[]
            {
                new Reservation("USER1", 22.September(2021)),
            };

            var dateInterval = new DateInterval(FirstDate, LastDate);

            var requestRepository = new RequestRepositoryBuilder()
                .WithGetRequests(dateInterval, requests)
                .Build();

            var reservationRepository = new ReservationRepositoryBuilder()
                .WithGetReservations(dateInterval, reservations)
                .Build();

            var controller = new HistoryController(
                CreateDefaultDateCalculator(),
                requestRepository,
                reservationRepository);

            var result = await controller.GetAsync("USER1", LastDate);

            var actual = GetResultValue<HistoryResponse>(result);

            Assert.Equal(1, actual.AllocatedContestedRequestsCount);
            Assert.Equal(4, actual.TotalContestedRequestsCount);
            Assert.Equal(0.25m, actual.AllocationRatio);
        }

        private static IDateCalculator CreateDefaultDateCalculator()
        {
            var mockDateCalculator = new Mock<IDateCalculator>();
            mockDateCalculator
                .Setup(d => d.IsWorkingDay(It.IsAny<LocalDate>()))
                .Returns(true);

            return mockDateCalculator.Object;
        }

        private static IReservationRepository CreateDefaultReservationRepository()
        {
            var mockReservationRepository = new Mock<IReservationRepository>();
            mockReservationRepository
                .Setup(r => r.GetReservations(It.IsAny<DateInterval>()))
                .ReturnsAsync(new List<Reservation>());

            return mockReservationRepository.Object;
        }

        private static IRequestRepository CreateDefaultRequestRepository()
        {
            var mockRequestRepository = new Mock<IRequestRepository>();
            mockRequestRepository
                .Setup(r => r.GetRequests(It.IsAny<DateInterval>()))
                .ReturnsAsync(new List<Request>());

            return mockRequestRepository.Object;
        }
    }
}