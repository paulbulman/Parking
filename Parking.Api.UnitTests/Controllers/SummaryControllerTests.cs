namespace Parking.Api.UnitTests.Controllers;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Controllers;
using Api.Json.Summary;
using Business;
using Model;
using NodaTime.Testing.Extensions;
using TestHelpers;
using Xunit;
using static ControllerHelpers;
using static Json.Calendar.CalendarHelpers;
using CreateControllerContext = Helpers.CreateControllerContext;

public static class SummaryControllerTests
{
    [Fact]
    public static async Task Get_summary_returns_summary_data_for_each_active_date()
    {
        var activeDates = new[] { 28.June(2021), 29.June(2021), 1.July(2021) };

        var dateCalculator = CreateDateCalculator.WithActiveDates(activeDates);

        var requestRepository = new RequestRepositoryBuilder()
            .WithGetRequests("user1", activeDates.ToDateInterval(), new List<Request>())
            .Build();

        var controller = new SummaryController(dateCalculator, requestRepository)
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
    [InlineData(RequestStatus.Allocated, SummaryStatus.Allocated, false)]
    [InlineData(RequestStatus.HardInterrupted, SummaryStatus.HardInterrupted, true)]
    [InlineData(RequestStatus.Interrupted, SummaryStatus.Interrupted, true)]
    [InlineData(RequestStatus.Pending, SummaryStatus.Pending, false)]
    [InlineData(RequestStatus.SoftInterrupted, SummaryStatus.Interrupted, true)]
    public static async Task Get_summary_returns_request_status(
        RequestStatus requestStatus,
        SummaryStatus expectedSummaryStatus,
        bool expectedIsProblem)
    {
        var activeDates = new[] { 28.June(2021) };

        var dateCalculator = CreateDateCalculator.WithActiveDates(activeDates);

        var requests = new[] { new Request("user1", 28.June(2021), requestStatus) };

        var requestRepository = new RequestRepositoryBuilder()
            .WithGetRequests("user1", activeDates.ToDateInterval(), requests)
            .Build();

        var controller = new SummaryController(dateCalculator, requestRepository)
        {
            ControllerContext = CreateControllerContext.WithUsername("user1")
        };

        var result = await controller.GetSummaryAsync();

        var resultValue = GetResultValue<SummaryResponse>(result);

        var data = GetDailyData(resultValue.Summary, 28.June(2021));

        Assert.Equal(expectedSummaryStatus, data.Status);
        Assert.Equal(expectedIsProblem, data.IsProblem);
    }

    [Fact]
    public static async Task Get_summary_returns_null_status_when_no_request_exists()
    {
        var activeDates = new[] { 28.June(2021) };

        var dateCalculator = CreateDateCalculator.WithActiveDates(activeDates);

        var requestRepository = new RequestRepositoryBuilder()
            .WithGetRequests("user1", activeDates.ToDateInterval(), new List<Request>())
            .Build();

        var controller = new SummaryController(dateCalculator, requestRepository)
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

        var dateCalculator = CreateDateCalculator.WithActiveDates(activeDates);

        var requests = new[] { new Request("user1", 28.June(2021), RequestStatus.Cancelled) };

        var requestRepository = new RequestRepositoryBuilder()
            .WithGetRequests("user1", activeDates.ToDateInterval(), requests)
            .Build();

        var controller = new SummaryController(dateCalculator, requestRepository)
        {
            ControllerContext = CreateControllerContext.WithUsername("user1")
        };

        var result = await controller.GetSummaryAsync();

        var resultValue = GetResultValue<SummaryResponse>(result);

        var data = GetDailyData(resultValue.Summary, 28.June(2021));

        Assert.Null(data.Status);
        Assert.False(data.IsProblem);
    }
}