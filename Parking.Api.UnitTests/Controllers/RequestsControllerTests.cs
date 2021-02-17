namespace Parking.Api.UnitTests.Controllers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Api.Controllers;
    using Api.Json.Calendar;
    using Api.Json.Requests;
    using Business.Data;
    using Model;
    using Moq;
    using NodaTime;
    using NodaTime.Testing.Extensions;
    using TestHelpers;
    using Xunit;
    using static ControllerHelpers;

    public static class RequestsControllerTests
    {
        private const string UserId = "User1";

        [Fact]
        public static async Task Returns_requests_data_for_each_active_date()
        {
            var activeDates = new[] { 2.February(2021), 3.February(2021), 4.February(2021) };

            var mockRequestRepository = new Mock<IRequestRepository>(MockBehavior.Strict);
            mockRequestRepository
                .Setup(r => r.GetRequests(UserId, 2.February(2021), 4.February(2021)))
                .ReturnsAsync(new List<Request>());

            var controller = new RequestsController(
                CreateDateCalculator.WithActiveDates(activeDates),
                mockRequestRepository.Object)
            {
                ControllerContext = CreateControllerContext.WithUsername(UserId)
            };

            var result = await controller.GetAsync();

            var calendar = GetResultValue<Calendar<RequestsData>>(result);

            var visibleDays = GetAllDays(calendar)
                .Where(d => !d.Hidden)
                .ToArray();

            Assert.Equal(activeDates, visibleDays.Select(d => d.LocalDate));

            Assert.All(visibleDays, d => Assert.NotNull(d.Data));
        }

        [Theory]
        [InlineData(RequestStatus.Requested)]
        [InlineData(RequestStatus.Allocated)]
        public static async Task Returns_true_when_space_has_been_requested(RequestStatus requestStatus)
        {
            var activeDates = new[] { 2.February(2021) };
            
            var requests = new[] { new Request(UserId, 2.February(2021), requestStatus) };

            var controller = new RequestsController(
                CreateDateCalculator.WithActiveDates(activeDates),
                CreateRequestRepository.WithRequests(UserId, activeDates, requests))
            {
                ControllerContext = CreateControllerContext.WithUsername(UserId)
            };

            var result = await controller.GetAsync();

            var calendar = GetResultValue<Calendar<RequestsData>>(result);

            var requested = GetDay(calendar, 2.February(2021)).Data.Requested;

            Assert.True(requested);
        }

        [Fact]
        public static async Task Returns_false_when_space_has_not_been_requested()
        {
            var activeDates = new[] { 2.February(2021) };

            var controller = new RequestsController(
                CreateDateCalculator.WithActiveDates(activeDates),
                CreateRequestRepository.WithRequests(UserId, activeDates, new List<Request>()))
            {
                ControllerContext = CreateControllerContext.WithUsername(UserId)
            };

            var result = await controller.GetAsync();

            var calendar = GetResultValue<Calendar<RequestsData>>(result);

            var requested = GetDay(calendar, 2.February(2021)).Data.Requested;

            Assert.False(requested);
        }

        [Fact]
        public static async Task Returns_false_when_space_has_been_cancelled()
        {
            var activeDates = new[] { 2.February(2021) };

            var requests = new[] { new Request(UserId, 2.February(2021), RequestStatus.Cancelled) };

            var controller = new RequestsController(
                CreateDateCalculator.WithActiveDates(activeDates),
                CreateRequestRepository.WithRequests(UserId, activeDates, requests))
            {
                ControllerContext = CreateControllerContext.WithUsername(UserId)
            };

            var result = await controller.GetAsync();

            var calendar = GetResultValue<Calendar<RequestsData>>(result);

            var requested = GetDay(calendar, 2.February(2021)).Data.Requested;

            Assert.False(requested);
        }

        private static IEnumerable<Day<RequestsData>> GetAllDays(Calendar<RequestsData> calendar) =>
            calendar.Weeks.SelectMany(w => w.Days);

        private static Day<RequestsData> GetDay(Calendar<RequestsData> calendar, LocalDate localDate) =>
            calendar.Weeks.SelectMany(w => w.Days).Single(d => d.LocalDate == localDate);
    }
}