// ReSharper disable StringLiteralTypo
namespace Parking.Api.UnitTests.Controllers;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Controllers;
using Api.Json.DailyDetails;
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
using CreateControllerContext = Helpers.CreateControllerContext;

public static class DailyDetailsControllerTests
{
    [Fact]
    public static async Task Returns_details_data_for_each_active_date()
    {
        var activeDates = new[] {12.July(2021), 13.July(2021), 16.July(2021)};

        var dateCalculator = CreateDateCalculator.WithActiveDates(activeDates);

        var requestRepository = new RequestRepositoryBuilder()
            .WithGetRequests(activeDates.ToDateInterval(), new List<Request>())
            .Build();

        var controller = new DailyDetailsController(
            dateCalculator,
            requestRepository,
            Mock.Of<ITriggerRepository>(),
            CreateUserRepository.WithUsers(new List<User>()))
        {
            ControllerContext = CreateControllerContext.WithUsername("user1")
        };

        var result = await controller.GetAsync();

        var resultValue = GetResultValue<DailyDetailsResponse>(result);

        Assert.Equal(activeDates, resultValue.Details.Select(d => d.LocalDate));
    }

    [Fact]
    public static async Task Groups_allocated_and_interrupted_users_sorted_by_last_name()
    {
        var activeDates = new[] {12.July(2021)};

        var dateCalculator = CreateDateCalculator.WithActiveDates(activeDates);

        var users = new[]
        {
            CreateUser.With(userId: "user1", firstName: "Cathie", lastName: "Phoenix"),
            CreateUser.With(userId: "user2", firstName: "Hynda", lastName: "Lindback"),
            CreateUser.With(userId: "user3", firstName: "Shannen", lastName: "Muddicliffe"),
            CreateUser.With(userId: "user4", firstName: "Marco", lastName: "Call"),
            CreateUser.With(userId: "user5", firstName: "Eugenio", lastName: "Veazey"),
            CreateUser.With(userId: "user6", firstName: "Evangelin", lastName: "Calway"),
        };

        var requests = new[]
        {
            new Request("user1", 12.July(2021), RequestStatus.Allocated),
            new Request("user2", 12.July(2021), RequestStatus.Allocated),
            new Request("user3", 12.July(2021), RequestStatus.Interrupted),
            new Request("user4", 12.July(2021), RequestStatus.SoftInterrupted),
            new Request("user5", 12.July(2021), RequestStatus.Pending),
            new Request("user6", 12.July(2021), RequestStatus.Pending),
        };

        var requestRepository = new RequestRepositoryBuilder()
            .WithGetRequests(activeDates.ToDateInterval(), requests)
            .Build();

        var controller = new DailyDetailsController(
            dateCalculator,
            requestRepository,
            Mock.Of<ITriggerRepository>(),
            CreateUserRepository.WithUsers(users))
        {
            ControllerContext = CreateControllerContext.WithUsername("user1")
        };

        var result = await controller.GetAsync();

        var resultValue = GetResultValue<DailyDetailsResponse>(result);

        var data = GetDailyData(resultValue.Details, 12.July(2021));

        Assert.Equal(new[] {"Hynda Lindback", "Cathie Phoenix"}, data.AllocatedUsers.Select(u => u.Name));
        Assert.Equal(new[] {"Marco Call", "Shannen Muddicliffe"}, data.InterruptedUsers.Select(u => u.Name));
        Assert.Equal(new[] { "Evangelin Calway", "Eugenio Veazey" }, data.PendingUsers.Select(u => u.Name));
    }

    [Fact]
    public static async Task Ignores_cancelled_requests()
    {
        var activeDates = new[] { 16.July(2021), 17.July(2021) };

        var dateCalculator = CreateDateCalculator.WithActiveDates(activeDates);

        var users = new[]
        {
            CreateUser.With(userId: "user1", firstName: "Cathie", lastName: "Phoenix"),
            CreateUser.With(userId: "user2", firstName: "Hynda", lastName: "Lindback"),
            CreateUser.With(userId: "user3", firstName: "Shannen", lastName: "Muddicliffe"),
        };

        var requests = new[]
        {
            new Request("user1", 16.July(2021), RequestStatus.Cancelled),
            new Request("user2", 16.July(2021), RequestStatus.Cancelled),
            new Request("user3", 17.July(2021), RequestStatus.Cancelled),
        };

        var requestRepository = new RequestRepositoryBuilder()
            .WithGetRequests(activeDates.ToDateInterval(), requests)
            .Build();

        var controller = new DailyDetailsController(
            dateCalculator,
            requestRepository,
            Mock.Of<ITriggerRepository>(),
            CreateUserRepository.WithUsers(users))
        {
            ControllerContext = CreateControllerContext.WithUsername("user1")
        };

        var result = await controller.GetAsync();

        var resultValue = GetResultValue<DailyDetailsResponse>(result);

        var day1Data = GetDailyData(resultValue.Details, 16.July(2021));
        var day2Data = GetDailyData(resultValue.Details, 17.July(2021));

        Assert.Empty(day1Data.AllocatedUsers);
        Assert.Empty(day1Data.InterruptedUsers);
        Assert.Empty(day2Data.PendingUsers);
    }

    [Fact]
    public static async Task Highlights_active_user()
    {
        var activeDates = new[] { 16.July(2021), 17.July(2021), 18.July(2021) };

        var dateCalculator = CreateDateCalculator.WithActiveDates(activeDates);

        var users = new[]
        {
            CreateUser.With(userId: "user1", firstName: "Cathie", lastName: "Phoenix"),
            CreateUser.With(userId: "user2", firstName: "Hynda", lastName: "Lindback"),
        };

        var requests = new[]
        {
            new Request("user1", 16.July(2021), RequestStatus.Allocated),
            new Request("user2", 16.July(2021), RequestStatus.SoftInterrupted),
            new Request("user1", 17.July(2021), RequestStatus.Interrupted),
            new Request("user2", 17.July(2021), RequestStatus.Allocated),
            new Request("user2", 18.July(2021), RequestStatus.Pending),
        };

        var requestRepository = new RequestRepositoryBuilder()
            .WithGetRequests(activeDates.ToDateInterval(), requests)
            .Build();

        var controller = new DailyDetailsController(
            dateCalculator,
            requestRepository,
            Mock.Of<ITriggerRepository>(),
            CreateUserRepository.WithUsers(users))
        {
            ControllerContext = CreateControllerContext.WithUsername("user2")
        };

        var result = await controller.GetAsync();

        var resultValue = GetResultValue<DailyDetailsResponse>(result);

        var day1Data = GetDailyData(resultValue.Details, 16.July(2021));
        var day2Data = GetDailyData(resultValue.Details, 17.July(2021));
        var day3Data = GetDailyData(resultValue.Details, 18.July(2021));

        Assert.False(day1Data.AllocatedUsers.Single().IsHighlighted);
        Assert.True(day1Data.InterruptedUsers.Single().IsHighlighted);

        Assert.True(day2Data.AllocatedUsers.Single().IsHighlighted);
        Assert.False(day2Data.InterruptedUsers.Single().IsHighlighted);

        Assert.True(day3Data.PendingUsers.Single().IsHighlighted);
    }

    [Theory]
    [InlineData(RequestStatus.Allocated, false, false)]
    [InlineData(RequestStatus.Cancelled, false, false)]
    [InlineData(RequestStatus.HardInterrupted, true, true)]
    [InlineData(RequestStatus.Interrupted, false, false)]
    [InlineData(RequestStatus.Pending, false, false)]
    [InlineData(RequestStatus.SoftInterrupted, true, false)]
    public static async Task Returns_stay_interrupted_status(
        RequestStatus currentUserRequestStatus,
        bool expectedIsAllowed,
        bool expectedIsSet)
    {
        var activeDates = new[] {12.July(2021)};

        var requests = new[]
        {
            new Request("user1", 12.July(2021), RequestStatus.SoftInterrupted),
            new Request("user2", 12.July(2021), currentUserRequestStatus),
        };

        var dateCalculator = CreateDateCalculator.WithActiveDates(activeDates);

        var users = new[]
        {
            CreateUser.With(userId: "user1", firstName: "Cathie", lastName: "Phoenix"),
            CreateUser.With(userId: "user2", firstName: "Hynda", lastName: "Lindback"),
        };

        var requestRepository = new RequestRepositoryBuilder()
            .WithGetRequests(activeDates.ToDateInterval(), requests)
            .Build();

        var controller = new DailyDetailsController(
            dateCalculator,
            requestRepository,
            Mock.Of<ITriggerRepository>(),
            CreateUserRepository.WithUsers(users))
        {
            ControllerContext = CreateControllerContext.WithUsername("user2")
        };

        var result = await controller.GetAsync();

        var resultValue = GetResultValue<DailyDetailsResponse>(result);

        var actual = GetDailyData(resultValue.Details, 12.July(2021)).StayInterruptedStatus;

        Assert.Equal(expectedIsAllowed, actual.IsAllowed);
        Assert.Equal(expectedIsSet, actual.IsSet);
    }

    [Fact]
    public static async Task Returns_stay_interrupted_status_when_no_user_requests_exist()
    {
        var activeDates = new[] {12.July(2021)};

        var dateCalculator = CreateDateCalculator.WithActiveDates(activeDates);

        var requests = new[]
        {
            new Request("user2", 12.July(2021), RequestStatus.SoftInterrupted),
        };

        var users = new[]
        {
            CreateUser.With(userId: "user1", firstName: "Cathie", lastName: "Phoenix"),
        };

        var requestRepository = new RequestRepositoryBuilder()
            .WithGetRequests(activeDates.ToDateInterval(), requests)
            .Build();

        var controller = new DailyDetailsController(
            dateCalculator,
            requestRepository,
            Mock.Of<ITriggerRepository>(),
            CreateUserRepository.WithUsers(users))
        {
            ControllerContext = CreateControllerContext.WithUsername("user1")
        };

        var result = await controller.GetAsync();

        var resultValue = GetResultValue<DailyDetailsResponse>(result);

        var actual = GetDailyData(resultValue.Details, 12.July(2021)).StayInterruptedStatus;

        Assert.False(actual.IsAllowed);
        Assert.False(actual.IsSet);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public static async Task Returns_404_response_when_existing_request_cannot_be_found(
        bool acceptInterruption)
    {
        var requestRepository = new RequestRepositoryBuilder()
            .WithGetRequests("user1", 12.July(2021).ToDateInterval(), new List<Request>())
            .Build();

        var controller = new DailyDetailsController(
            Mock.Of<IDateCalculator>(),
            requestRepository,
            Mock.Of<ITriggerRepository>(),
            Mock.Of<IUserRepository>())
        {
            ControllerContext = CreateControllerContext.WithUsername("user1")
        };

        var result = await controller.PatchAsync(
            new StayInterruptedPatchRequest(12.July(2021), acceptInterruption));

        Assert.IsType<NotFoundResult>(result);
    }

    [Theory]
    [InlineData(RequestStatus.Allocated, true)]
    [InlineData(RequestStatus.Cancelled, false)]
    [InlineData(RequestStatus.Interrupted, true)]
    public static async Task Returns_400_response_when_existing_request_cannot_be_updated(
        RequestStatus requestStatus,
        bool acceptInterruption)
    {
        var requests = new[] { new Request("user1", 12.July(2021), requestStatus) };

        var requestRepository = new RequestRepositoryBuilder()
            .WithGetRequests("user1", 12.July(2021).ToDateInterval(), requests)
            .Build();

        var controller = new DailyDetailsController(
            Mock.Of<IDateCalculator>(),
            requestRepository,
            Mock.Of<ITriggerRepository>(),
            Mock.Of<IUserRepository>())
        {
            ControllerContext = CreateControllerContext.WithUsername("user1")
        };

        var result = await controller.PatchAsync(
            new StayInterruptedPatchRequest(12.July(2021), acceptInterruption));

        Assert.IsType<BadRequestResult>(result);
    }

    [Theory]
    [InlineData(RequestStatus.SoftInterrupted, true, RequestStatus.HardInterrupted)]
    [InlineData(RequestStatus.HardInterrupted, false, RequestStatus.SoftInterrupted)]
    public static async Task Updates_interruption_status(
        RequestStatus initialRequestStatus,
        bool acceptInterruption,
        RequestStatus expectedRequestStatus)
    {
        var activeDates = new[] { 12.July(2021) };

        var dateCalculator = CreateDateCalculator.WithActiveDates(activeDates);

        var requestDate = activeDates.Single();

        var existingRequests = new[] { new Request("user1", requestDate, initialRequestStatus) };

        var mockRequestRepository = new RequestRepositoryBuilder()
            .WithGetRequests("user1", activeDates.ToDateInterval(), existingRequests)
            .WithGetRequests(activeDates.ToDateInterval(), new List<Request>())
            .BuildMock();

        var users = new[]
        {
            CreateUser.With(userId: "user1", firstName: "Cathie", lastName: "Phoenix"),
        };

        var controller = new DailyDetailsController(
            dateCalculator,
            mockRequestRepository.Object,
            Mock.Of<ITriggerRepository>(),
            CreateUserRepository.WithUsers(users))
        {
            ControllerContext = CreateControllerContext.WithUsername("user1")
        };

        await controller.PatchAsync(
            new StayInterruptedPatchRequest(requestDate, acceptInterruption));

        var expectedRequests = new[] { new Request("user1", requestDate, expectedRequestStatus) };

        mockRequestRepository.Verify(
            r => r.SaveRequests(
                It.Is<IReadOnlyCollection<Request>>(actual => CheckRequests(expectedRequests, actual.ToList()))),
            Times.Once);
    }

    [Fact]
    public static async Task Creates_recalculation_trigger_when_updating_interruption_status()
    {
        var activeDates = new[] { 28.June(2021) };

        var dateCalculator = CreateDateCalculator.WithActiveDates(activeDates);

        var requestDate = activeDates.Single();

        var existingRequests = new[] { new Request("user1", requestDate, RequestStatus.SoftInterrupted) };

        var requestRepository = new RequestRepositoryBuilder()
            .WithGetRequests("user1", activeDates.ToDateInterval(), existingRequests)
            .WithGetRequests(activeDates.ToDateInterval(), new List<Request>())
            .Build();

        var users = new[]
        {
            CreateUser.With(userId: "user1", firstName: "Cathie", lastName: "Phoenix"),
        };

        var mockTriggerRepository = new Mock<ITriggerRepository>();

        var controller = new DailyDetailsController(
            dateCalculator,
            requestRepository,
            mockTriggerRepository.Object,
            CreateUserRepository.WithUsers(users))
        {
            ControllerContext = CreateControllerContext.WithUsername("user1")
        };

        await controller.PatchAsync(new StayInterruptedPatchRequest(requestDate, true));

        mockTriggerRepository.Verify(r => r.AddTrigger(), Times.Once);
    }

    [Theory]
    [InlineData(RequestStatus.SoftInterrupted, RequestStatus.HardInterrupted, true)]
    [InlineData(RequestStatus.HardInterrupted, RequestStatus.SoftInterrupted, false)]
    public static async Task Returns_updated_daily_details_when_updating_interruption_status(
        RequestStatus initialRequestStatus,
        RequestStatus updatedRequestStatus,
        bool value)
    {
        var activeDates = new[] { 28.June(2021), 29.June(2021) };

        var dateCalculator = CreateDateCalculator.WithActiveDates(activeDates);

        var initialRequest = new Request("user1", 28.June(2021), initialRequestStatus);

        var updatedRequests = new[]
        {
            new Request("user1", 28.June(2021), updatedRequestStatus),
            new Request("user1", 29.June(2021), RequestStatus.Interrupted),
        };

        var requestRepository = new RequestRepositoryBuilder()
            .WithGetRequests("user1", 28.June(2021).ToDateInterval(), new[] { initialRequest })
            .WithGetRequests(activeDates.ToDateInterval(), updatedRequests)
            .Build();

        var users = new[]
        {
            CreateUser.With(userId: "user1", firstName: "Cathie", lastName: "Phoenix"),
        };

        var controller = new DailyDetailsController(
            dateCalculator,
            requestRepository,
            Mock.Of<ITriggerRepository>(),
            CreateUserRepository.WithUsers(users))
        {
            ControllerContext = CreateControllerContext.WithUsername("user1")
        };

        var result = await controller.PatchAsync(
            new StayInterruptedPatchRequest(28.June(2021), value));

        var resultValue = GetResultValue<DailyDetailsResponse>(result);

        var actual = GetDailyData(resultValue.Details, 28.June(2021)).StayInterruptedStatus;

        Assert.True(actual.IsAllowed);
        Assert.Equal(value, actual.IsSet);
    }

    private static bool CheckRequests(
        IReadOnlyCollection<Request> expected,
        IReadOnlyCollection<Request> actual) =>
        actual.Count == expected.Count && expected.All(e => actual.Contains(e, new RequestsComparer()));
}