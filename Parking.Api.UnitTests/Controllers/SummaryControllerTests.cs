namespace Parking.Api.UnitTests.Controllers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Api.Controllers;
    using Api.Json.Summary;
    using Model;
    using NodaTime.Testing.Extensions;
    using TestHelpers;
    using Xunit;
    using static ControllerHelpers;
    using static Json.Calendar.CalendarHelpers;

    public static class SummaryControllerTests
    {
        [Fact]
        public static async Task Returns_summary_data_for_each_active_date()
        {
            var activeDates = new[] { 28.June(2021), 29.June(2021), 1.July(2021) };
            var longLeadTimeAllocationDates = new[] { 1.July(2021) };

            var dateCalculator = CreateDateCalculator.WithActiveDatesAndLongLeadTimeAllocationDates(
                activeDates,
                longLeadTimeAllocationDates);

            var controller = new SummaryController(
                dateCalculator,
                CreateRequestRepository.WithRequests("user1", activeDates, new List<Request>()))
            {
                ControllerContext = CreateControllerContext.WithUsername("user1")
            };

            var result = await controller.GetAsync();

            var resultValue = GetResultValue<SummaryResponse>(result);

            var visibleDays = GetVisibleDays(resultValue.Summary);

            Assert.Equal(activeDates, visibleDays.Select(d => d.LocalDate));

            Assert.All(visibleDays, d => Assert.NotNull(d.Data));
        }

        [Theory]
        [InlineData(RequestStatus.Allocated, SummaryStatus.Allocated)]
        [InlineData(RequestStatus.Requested, SummaryStatus.Requested)]
        public static async Task Returns_request_status(
            RequestStatus requestStatus,
            SummaryStatus expectedSummaryStatus)
        {
            var activeDates = new[] { 28.June(2021) };
            var longLeadTimeAllocationDates = new[] { 27.June(2021) };

            var requests = new[] { new Request("user1", 28.June(2021), requestStatus) };

            var dateCalculator = CreateDateCalculator.WithActiveDatesAndLongLeadTimeAllocationDates(
                activeDates,
                longLeadTimeAllocationDates);

            var controller = new SummaryController(
                dateCalculator,
                CreateRequestRepository.WithRequests("user1", activeDates, requests))
            {
                ControllerContext = CreateControllerContext.WithUsername("user1")
            };

            var result = await controller.GetAsync();

            var resultValue = GetResultValue<SummaryResponse>(result);

            var data = GetDailyData(resultValue.Summary, 28.June(2021));

            Assert.Equal(expectedSummaryStatus, data.Status);
            Assert.False(data.IsProblem);
        }

        [Fact]
        public static async Task Returns_unallocated_requests_within_long_lead_time_as_interrupted()
        {
            var activeDates = new[] { 28.June(2021) };
            var longLeadTimeAllocationDates = new[] { 1.July(2021) };

            var requests = new[] { new Request("user1", 28.June(2021), RequestStatus.Requested) };

            var dateCalculator = CreateDateCalculator.WithActiveDatesAndLongLeadTimeAllocationDates(
                activeDates,
                longLeadTimeAllocationDates);

            var controller = new SummaryController(
                dateCalculator,
                CreateRequestRepository.WithRequests("user1", activeDates, requests))
            {
                ControllerContext = CreateControllerContext.WithUsername("user1")
            };

            var result = await controller.GetAsync();

            var resultValue = GetResultValue<SummaryResponse>(result);

            var data = GetDailyData(resultValue.Summary, 28.June(2021));

            Assert.Equal(SummaryStatus.Interrupted, data.Status);
            Assert.True(data.IsProblem);
        }

        [Fact]
        public static async Task Returns_null_status_when_no_request_exists()
        {
            var activeDates = new[] { 28.June(2021) };
            var longLeadTimeAllocationDates = new[] { 1.July(2021) };

            var dateCalculator = CreateDateCalculator.WithActiveDatesAndLongLeadTimeAllocationDates(
                activeDates,
                longLeadTimeAllocationDates);

            var controller = new SummaryController(
                dateCalculator,
                CreateRequestRepository.WithRequests("user1", activeDates, new List<Request>()))
            {
                ControllerContext = CreateControllerContext.WithUsername("user1")
            };

            var result = await controller.GetAsync();

            var resultValue = GetResultValue<SummaryResponse>(result);

            var data = GetDailyData(resultValue.Summary, 28.June(2021));

            Assert.Null(data.Status);
            Assert.False(data.IsProblem);
        }

        [Fact]
        public static async Task Ignores_cancelled_requests()
        {
            var activeDates = new[] { 28.June(2021) };
            var longLeadTimeAllocationDates = new[] { 1.July(2021) };

            var requests = new[] { new Request("user1", 28.June(2021), RequestStatus.Cancelled) };

            var dateCalculator = CreateDateCalculator.WithActiveDatesAndLongLeadTimeAllocationDates(
                activeDates,
                longLeadTimeAllocationDates);

            var controller = new SummaryController(
                dateCalculator,
                CreateRequestRepository.WithRequests("user1", activeDates, requests))
            {
                ControllerContext = CreateControllerContext.WithUsername("user1")
            };

            var result = await controller.GetAsync();

            var resultValue = GetResultValue<SummaryResponse>(result);

            var data = GetDailyData(resultValue.Summary, 28.June(2021));

            Assert.Null(data.Status);
            Assert.False(data.IsProblem);
        }
    }
}