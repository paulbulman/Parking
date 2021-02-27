namespace Parking.Api.UnitTests.Controllers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Api.Controllers;
    using Api.Json.Requests;
    using Model;
    using NodaTime.Testing.Extensions;
    using TestHelpers;
    using Xunit;
    using static ControllerHelpers;
    using static Json.Calendar.CalendarHelpers;

    public static class RequestsControllerTests
    {
        private const string UserId = "User1";

        [Fact]
        public static async Task Returns_requests_data_for_each_active_date()
        {
            var activeDates = new[] { 2.February(2021), 3.February(2021), 4.February(2021) };

            var controller = new RequestsController(
                CreateDateCalculator.WithActiveDates(activeDates),
                CreateRequestRepository.WithRequests(UserId, activeDates, new List<Request>()))
            {
                ControllerContext = CreateControllerContext.WithUsername(UserId)
            };

            var result = await controller.GetAsync();

            var resultValue = GetResultValue<RequestsResponse>(result);

            var visibleDays = GetVisibleDays(resultValue.Requests);

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

            var resultValue = GetResultValue<RequestsResponse>(result);

            var data = GetDailyData(resultValue.Requests, 2.February(2021));

            Assert.True(data.Requested);
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

            var resultValue = GetResultValue<RequestsResponse>(result);

            var data = GetDailyData(resultValue.Requests, 2.February(2021));

            Assert.False(data.Requested);
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

            var resultValue = GetResultValue<RequestsResponse>(result);

            var data = GetDailyData(resultValue.Requests, 2.February(2021));

            Assert.False(data.Requested);
        }

        [Fact]
        public static async Task Returns_data_for_given_user_when_specified()
        {
            var activeDates = new[] { 2.February(2021) };

            var requests = new[] { new Request(UserId, 2.February(2021), RequestStatus.Requested) };

            var controller = new RequestsController(
                CreateDateCalculator.WithActiveDates(activeDates),
                CreateRequestRepository.WithRequests(UserId, activeDates, requests));

            var result = await controller.GetAsync(UserId);

            var resultValue = GetResultValue<RequestsResponse>(result);

            var data = GetDailyData(resultValue.Requests, 2.February(2021));

            Assert.True(data.Requested);
        }
    }
}