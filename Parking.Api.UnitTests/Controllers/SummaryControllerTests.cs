namespace Parking.Api.UnitTests.Controllers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Api.Controllers;
    using Api.Json.Summary;
    using Business;
    using Business.Data;
    using Microsoft.AspNetCore.Mvc;
    using Model;
    using Moq;
    using NodaTime.Testing.Extensions;
    using TestHelpers;
    using Xunit;
    using static ControllerHelpers;
    using static Json.Calendar.CalendarHelpers;

    public static class SummaryControllerTests
    {
        [Fact]
        public static async Task Get_summary_returns_summary_data_for_each_active_date()
        {
            var activeDates = new[] { 28.June(2021), 29.June(2021), 1.July(2021) };
            var longLeadTimeAllocationDates = new[] { 1.July(2021) };

            var dateCalculator = CreateDateCalculator.WithActiveDatesAndLongLeadTimeAllocationDates(
                activeDates,
                longLeadTimeAllocationDates);

            var controller = new SummaryController(
                dateCalculator,
                CreateRequestRepository.WithRequests("user1", activeDates, new List<Request>()),
                Mock.Of<ITriggerRepository>())
            {
                ControllerContext = CreateControllerContext.WithUsername("user1")
            };

            var result = await controller.GetSummaryAsync();

            var resultValue = GetResultValue<SummaryResponse>(result);

            var visibleDays = GetVisibleDays(resultValue.Summary);

            Assert.Equal(activeDates, visibleDays.Select(d => d.LocalDate));

            Assert.All(visibleDays, d => Assert.NotNull(d.Data));
        }

        [Theory]
        [InlineData(RequestStatus.Allocated, SummaryStatus.Allocated)]
        [InlineData(RequestStatus.Requested, SummaryStatus.Requested)]
        public static async Task Get_summary_returns_request_status(
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
                CreateRequestRepository.WithRequests("user1", activeDates, requests),
                Mock.Of<ITriggerRepository>())
            {
                ControllerContext = CreateControllerContext.WithUsername("user1")
            };

            var result = await controller.GetSummaryAsync();

            var resultValue = GetResultValue<SummaryResponse>(result);

            var data = GetDailyData(resultValue.Summary, 28.June(2021));

            Assert.Equal(expectedSummaryStatus, data.Status);
            Assert.False(data.IsProblem);
        }

        [Theory]
        [InlineData(RequestStatus.Requested)]
        [InlineData(RequestStatus.SoftInterrupted)]
        [InlineData(RequestStatus.HardInterrupted)]
        public static async Task Get_summary_returns_unallocated_requests_within_long_lead_time_as_interrupted(
            RequestStatus requestStatus)
        {
            var activeDates = new[] { 28.June(2021) };
            var longLeadTimeAllocationDates = new[] { 1.July(2021) };

            var requests = new[] { new Request("user1", 28.June(2021), requestStatus) };

            var dateCalculator = CreateDateCalculator.WithActiveDatesAndLongLeadTimeAllocationDates(
                activeDates,
                longLeadTimeAllocationDates);

            var controller = new SummaryController(
                dateCalculator,
                CreateRequestRepository.WithRequests("user1", activeDates, requests),
                Mock.Of<ITriggerRepository>())
            {
                ControllerContext = CreateControllerContext.WithUsername("user1")
            };

            var result = await controller.GetSummaryAsync();

            var resultValue = GetResultValue<SummaryResponse>(result);

            var data = GetDailyData(resultValue.Summary, 28.June(2021));

            Assert.Equal(SummaryStatus.Interrupted, data.Status);
            Assert.True(data.IsProblem);
        }

        [Fact]
        public static async Task Get_summary_returns_null_status_when_no_request_exists()
        {
            var activeDates = new[] { 28.June(2021) };
            var longLeadTimeAllocationDates = new[] { 1.July(2021) };

            var dateCalculator = CreateDateCalculator.WithActiveDatesAndLongLeadTimeAllocationDates(
                activeDates,
                longLeadTimeAllocationDates);

            var controller = new SummaryController(
                dateCalculator,
                CreateRequestRepository.WithRequests("user1", activeDates, new List<Request>()),
                Mock.Of<ITriggerRepository>())
            {
                ControllerContext = CreateControllerContext.WithUsername("user1")
            };

            var result = await controller.GetSummaryAsync();

            var resultValue = GetResultValue<SummaryResponse>(result);

            var data = GetDailyData(resultValue.Summary, 28.June(2021));

            Assert.Null(data.Status);
            Assert.False(data.IsProblem);
        }

        [Fact]
        public static async Task Get_summary_ignores_cancelled_requests()
        {
            var activeDates = new[] { 28.June(2021) };
            var longLeadTimeAllocationDates = new[] { 1.July(2021) };

            var requests = new[] { new Request("user1", 28.June(2021), RequestStatus.Cancelled) };

            var dateCalculator = CreateDateCalculator.WithActiveDatesAndLongLeadTimeAllocationDates(
                activeDates,
                longLeadTimeAllocationDates);

            var controller = new SummaryController(
                dateCalculator,
                CreateRequestRepository.WithRequests("user1", activeDates, requests),
                Mock.Of<ITriggerRepository>())
            {
                ControllerContext = CreateControllerContext.WithUsername("user1")
            };

            var result = await controller.GetSummaryAsync();

            var resultValue = GetResultValue<SummaryResponse>(result);

            var data = GetDailyData(resultValue.Summary, 28.June(2021));

            Assert.Null(data.Status);
            Assert.False(data.IsProblem);
        }

        [Theory]
        [InlineData(RequestStatus.Requested, false, false)]
        [InlineData(RequestStatus.Allocated, false, false)]
        [InlineData(RequestStatus.Cancelled, false, false)]
        [InlineData(RequestStatus.SoftInterrupted, true, false)]
        [InlineData(RequestStatus.HardInterrupted, true, true)]
        public static async Task Get_summary_returns_stay_interrupted_status(
            RequestStatus firstRequestStatus,
            bool expectedIsAllowed,
            bool expectedIsSet)
        {
            var activeDates = new[] { 28.June(2021), 29.June(2021) };
            var longLeadTimeAllocationDates = new[] { 1.July(2021) };

            var requests = new[]
            {
                new Request("user1", 28.June(2021), firstRequestStatus),
                new Request("user1", 29.June(2021), RequestStatus.Requested),
            };

            var dateCalculator = CreateDateCalculator.WithActiveDatesAndLongLeadTimeAllocationDates(
                activeDates,
                longLeadTimeAllocationDates);

            var controller = new SummaryController(
                dateCalculator,
                CreateRequestRepository.WithRequests("user1", activeDates, requests),
                Mock.Of<ITriggerRepository>())
            {
                ControllerContext = CreateControllerContext.WithUsername("user1")
            };

            var result = await controller.GetSummaryAsync();

            var resultValue = GetResultValue<SummaryResponse>(result);

            var actual = resultValue.StayInterruptedStatus;

            Assert.Equal(28.June(2021), actual.LocalDate);
            Assert.Equal(expectedIsAllowed, actual.IsAllowed);
            Assert.Equal(expectedIsSet, actual.IsSet);
        }

        [Fact]
        public static async Task Get_summary_returns_stay_interrupted_status_when_no_requests_exist()
        {
            var activeDates = new[] { 29.June(2021) };
            var longLeadTimeAllocationDates = new[] { 1.July(2021) };

            var dateCalculator = CreateDateCalculator.WithActiveDatesAndLongLeadTimeAllocationDates(
                activeDates,
                longLeadTimeAllocationDates);

            var controller = new SummaryController(
                dateCalculator,
                CreateRequestRepository.WithRequests("user1", activeDates, new List<Request>()),
                Mock.Of<ITriggerRepository>())
            {
                ControllerContext = CreateControllerContext.WithUsername("user1")
            };

            var result = await controller.GetSummaryAsync();

            var resultValue = GetResultValue<SummaryResponse>(result);

            var actual = resultValue.StayInterruptedStatus;

            Assert.Equal(29.June(2021), actual.LocalDate);
            Assert.False(actual.IsAllowed);
            Assert.False(actual.IsSet);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task Update_interruption_status_returns_404_response_when_existing_request_cannot_be_found(
            bool acceptInterruption)
        {
            var requestDate = 28.June(2021);

            var controller = new SummaryController(
                Mock.Of<IDateCalculator>(),
                CreateRequestRepository.WithRequests("user1", requestDate, new List<Request>()),
                Mock.Of<ITriggerRepository>())
            {
                ControllerContext = CreateControllerContext.WithUsername("user1")
            };

            var result = await controller.UpdateStayInterruptedAsync(
                new StayInterruptedPatchRequest(requestDate, acceptInterruption));

            Assert.IsType<NotFoundResult>(result);
        }

        [Theory]
        [InlineData(RequestStatus.Allocated, true)]
        [InlineData(RequestStatus.Cancelled, false)]
        [InlineData(RequestStatus.Requested, true)]
        public static async Task Update_interruption_status_returns_400_response_when_existing_request_cannot_be_updated(
            RequestStatus requestStatus,
            bool acceptInterruption)
        {
            var requestDate = 28.June(2021);

            var requests = new[] { new Request("user1", requestDate, requestStatus) };

            var controller = new SummaryController(
                Mock.Of<IDateCalculator>(),
                CreateRequestRepository.WithRequests("user1", requestDate, requests),
                Mock.Of<ITriggerRepository>())
            {
                ControllerContext = CreateControllerContext.WithUsername("user1")
            };

            var result = await controller.UpdateStayInterruptedAsync(
                new StayInterruptedPatchRequest(requestDate, acceptInterruption));

            Assert.IsType<BadRequestResult>(result);
        }

        [Theory]
        [InlineData(RequestStatus.SoftInterrupted, true, RequestStatus.HardInterrupted)]
        [InlineData(RequestStatus.HardInterrupted, false, RequestStatus.SoftInterrupted)]
        public static async Task Update_interruption_status_updates_request(
            RequestStatus initialRequestStatus,
            bool acceptInterruption,
            RequestStatus expectedRequestStatus)
        {
            var activeDates = new[] { 28.June(2021) };
            var longLeadTimeAllocationDates = new[] { 1.July(2021) };

            var dateCalculator = CreateDateCalculator.WithActiveDatesAndLongLeadTimeAllocationDates(
                activeDates,
                longLeadTimeAllocationDates);

            var requestDate = activeDates.Single();

            var existingRequests = new[] { new Request("user1", requestDate, initialRequestStatus) };

            var mockRequestRepository = CreateRequestRepository.MockWithRequests("user1", activeDates, existingRequests);

            mockRequestRepository
                .Setup(r => r.SaveRequests(It.IsAny<IReadOnlyCollection<Request>>()))
                .Returns(Task.CompletedTask);

            var controller = new SummaryController(
                dateCalculator,
                mockRequestRepository.Object,
                Mock.Of<ITriggerRepository>())
            {
                ControllerContext = CreateControllerContext.WithUsername("user1")
            };

            await controller.UpdateStayInterruptedAsync(
                new StayInterruptedPatchRequest(requestDate, acceptInterruption));

            var expectedRequests = new[] { new Request("user1", requestDate, expectedRequestStatus) };

            mockRequestRepository.Verify(r => r.SaveRequests(
                    It.Is<IReadOnlyCollection<Request>>(actual => CheckRequests(expectedRequests, actual.ToList()))),
                Times.Once);
        }

        [Fact]
        public static async Task Update_interruption_creates_recalculation_trigger()
        {
            var activeDates = new[] { 28.June(2021) };
            var longLeadTimeAllocationDates = new[] { 1.July(2021) };

            var dateCalculator = CreateDateCalculator.WithActiveDatesAndLongLeadTimeAllocationDates(
                activeDates,
                longLeadTimeAllocationDates);

            var requestDate = activeDates.Single();

            var existingRequests = new[] { new Request("user1", requestDate, RequestStatus.SoftInterrupted) };

            var mockRequestRepository = CreateRequestRepository.MockWithRequests("user1", activeDates, existingRequests);

            mockRequestRepository
                .Setup(r => r.SaveRequests(It.IsAny<IReadOnlyCollection<Request>>()))
                .Returns(Task.CompletedTask);

            var mockTriggerRepository = new Mock<ITriggerRepository>();

            var controller = new SummaryController(
                dateCalculator,
                mockRequestRepository.Object,
                mockTriggerRepository.Object)
            {
                ControllerContext = CreateControllerContext.WithUsername("user1")
            };

            await controller.UpdateStayInterruptedAsync(new StayInterruptedPatchRequest(requestDate, true));

            mockTriggerRepository.Verify(r => r.AddTrigger(), Times.Once);
        }

        [Theory]
        [InlineData(RequestStatus.SoftInterrupted, RequestStatus.HardInterrupted, true)]
        [InlineData(RequestStatus.HardInterrupted, RequestStatus.SoftInterrupted, false)]
        public static async Task Update_interruption_status_returns_updated_summary(
            RequestStatus initialRequestStatus,
            RequestStatus updatedRequestStatus,
            bool value)
        {
            var activeDates = new[] { 28.June(2021), 29.June(2021) };
            var longLeadTimeAllocationDates = new[] { 1.July(2021) };

            var dateCalculator = CreateDateCalculator.WithActiveDatesAndLongLeadTimeAllocationDates(
                activeDates,
                longLeadTimeAllocationDates);

            var initialRequest = new Request("user1", 28.June(2021), initialRequestStatus);

            var updatedRequests = new[]
            {
                new Request("user1", 28.June(2021), updatedRequestStatus),
                new Request("user1", 29.June(2021), RequestStatus.Requested),
            };

            var mockRequestRepository = CreateRequestRepository.MockWithRequests("user1", activeDates, updatedRequests);

            mockRequestRepository
                .Setup(r => r.GetRequests("user1", 28.June(2021), 28.June(2021)))
                .ReturnsAsync(new[] {initialRequest});
            
            mockRequestRepository
                .Setup(r => r.SaveRequests(It.IsAny<IReadOnlyCollection<Request>>()))
                .Returns(Task.CompletedTask);

            var controller = new SummaryController(
                dateCalculator,
                mockRequestRepository.Object,
                Mock.Of<ITriggerRepository>())
            {
                ControllerContext = CreateControllerContext.WithUsername("user1")
            };

            var result = await controller.UpdateStayInterruptedAsync(
                new StayInterruptedPatchRequest(28.June(2021), value));

            var resultValue = GetResultValue<SummaryResponse>(result);

            var actual = resultValue.StayInterruptedStatus;

            Assert.Equal(28.June(2021), actual.LocalDate);
            Assert.True(actual.IsAllowed);
            Assert.Equal(value, actual.IsSet);
        }

        private static bool CheckRequests(
            IReadOnlyCollection<Request> expected,
            IReadOnlyCollection<Request> actual) =>
            actual.Count == expected.Count && expected.All(e => actual.Contains(e, new RequestsComparer()));
    }
}