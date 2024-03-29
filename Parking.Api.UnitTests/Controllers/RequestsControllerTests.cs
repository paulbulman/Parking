﻿namespace Parking.Api.UnitTests.Controllers;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Controllers;
using Api.Json.Requests;
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

public static class RequestsControllerTests
{
    private const string UserId = "User1";

    private static readonly IReadOnlyCollection<User> DefaultUsers =
    [
        CreateUser.With(userId: "user1", firstName: "User", lastName: "1"),
    ];

    [Fact]
    public static async Task Returns_requests_data_for_each_active_date()
    {
        var activeDates = new[] { 2.February(2021), 3.February(2021), 4.February(2021) };

        var requestRepository = new RequestRepositoryBuilder()
            .WithGetRequests(UserId, activeDates.ToDateInterval(), [])
            .Build();

        var controller = new RequestsController(
            CreateDateCalculator.WithActiveDates(activeDates),
            requestRepository,
            Mock.Of<ITriggerRepository>(),
            CreateUserRepository.WithUserExists(UserId, true))
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
    [InlineData(RequestStatus.Allocated)]
    [InlineData(RequestStatus.HardInterrupted)]
    [InlineData(RequestStatus.Interrupted)]
    [InlineData(RequestStatus.Pending)]
    [InlineData(RequestStatus.SoftInterrupted)]
    public static async Task Returns_true_when_space_has_been_requested(RequestStatus requestStatus)
    {
        var activeDates = new[] { 2.February(2021) };

        var requests = new[] { new Request(UserId, 2.February(2021), requestStatus) };

        var requestRepository = new RequestRepositoryBuilder()
            .WithGetRequests(UserId, activeDates.ToDateInterval(), requests)
            .Build();

        var controller = new RequestsController(
            CreateDateCalculator.WithActiveDates(activeDates),
            requestRepository,
            Mock.Of<ITriggerRepository>(),
            CreateUserRepository.WithUserExists(UserId, true))
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

        var requestRepository = new RequestRepositoryBuilder()
            .WithGetRequests(UserId, activeDates.ToDateInterval(), [])
            .Build();

        var controller = new RequestsController(
            CreateDateCalculator.WithActiveDates(activeDates),
            requestRepository,
            Mock.Of<ITriggerRepository>(),
            CreateUserRepository.WithUserExists(UserId, true))
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

        var requestRepository = new RequestRepositoryBuilder()
            .WithGetRequests(UserId, activeDates.ToDateInterval(), requests)
            .Build();

        var controller = new RequestsController(
            CreateDateCalculator.WithActiveDates(activeDates),
            requestRepository,
            Mock.Of<ITriggerRepository>(),
            CreateUserRepository.WithUserExists(UserId, true))
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

        var requests = new[] { new Request(UserId, 2.February(2021), RequestStatus.Interrupted) };

        var requestRepository = new RequestRepositoryBuilder()
            .WithGetRequests(UserId, activeDates.ToDateInterval(), requests)
            .Build();

        var controller = new RequestsController(
            CreateDateCalculator.WithActiveDates(activeDates),
            requestRepository,
            Mock.Of<ITriggerRepository>(),
            CreateUserRepository.WithUserExists(UserId, true));

        var result = await controller.GetByIdAsync(UserId);

        var resultValue = GetResultValue<RequestsResponse>(result);

        var data = GetDailyData(resultValue.Requests, 2.February(2021));

        Assert.True(data.Requested);
    }

    [Fact]
    public static async Task Returns_404_response_when_given_user_does_not_exist()
    {
        var controller = new RequestsController(
            Mock.Of<IDateCalculator>(),
            Mock.Of<IRequestRepository>(),
            Mock.Of<ITriggerRepository>(),
            CreateUserRepository.WithUserExists(UserId, false));

        var result = await controller.GetByIdAsync(UserId);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public static async Task Saves_requests_with_corresponding_state()
    {
        var activeDates = new[] { 2.February(2021), 3.February(2021) };

        var mockRequestRepository = new RequestRepositoryBuilder()
            .WithGetRequests(UserId, activeDates.ToDateInterval(), [])
            .BuildMock();

        var patchRequest = new RequestsPatchRequest(
        [
            new RequestsPatchRequestDailyData(2.February(2021), true),
            new RequestsPatchRequestDailyData(3.February(2021), false),
        ]);

        var controller = new RequestsController(
            CreateDateCalculator.WithActiveDates(activeDates),
            mockRequestRepository.Object,
            Mock.Of<ITriggerRepository>(),
            CreateUserRepository.WithUsers(DefaultUsers))
        {
            ControllerContext = CreateControllerContext.WithUsername(UserId)
        };

        await controller.PatchAsync(patchRequest);

        var expectedSavedRequests = new[]
        {
            new Request(UserId, 2.February(2021), RequestStatus.Pending),
            new Request(UserId, 3.February(2021), RequestStatus.Cancelled),
        };

        CheckSavedRequests(mockRequestRepository, expectedSavedRequests);
    }

    [Fact]
    public static async Task Adds_recalculation_trigger_when_saving_requests()
    {
        var activeDates = new[] { 2.February(2021) };

        var requestRepository = new RequestRepositoryBuilder()
            .WithGetRequests(UserId, activeDates.ToDateInterval(), [])
            .Build();

        var mockTriggerRepository = new Mock<ITriggerRepository>();
            
        var patchRequest = new RequestsPatchRequest([]);

        var controller = new RequestsController(
            CreateDateCalculator.WithActiveDates(activeDates),
            requestRepository,
            mockTriggerRepository.Object,
            CreateUserRepository.WithUsers(DefaultUsers))
        {
            ControllerContext = CreateControllerContext.WithUsername(UserId)
        };

        await controller.PatchAsync(patchRequest);

        mockTriggerRepository.Verify(r => r.AddTrigger(), Times.Once);
    }

    [Fact]
    public static async Task Updates_requests_with_last_given_value_for_each_date()
    {
        var activeDates = new[] { 2.February(2021), 3.February(2021) };

        var mockRequestRepository = new RequestRepositoryBuilder()
            .WithGetRequests(UserId, activeDates.ToDateInterval(), [])
            .BuildMock();

        var patchRequest = new RequestsPatchRequest(
        [
            new RequestsPatchRequestDailyData(2.February(2021), true),
            new RequestsPatchRequestDailyData(2.February(2021), false),
            new RequestsPatchRequestDailyData(2.February(2021), true),
            new RequestsPatchRequestDailyData(3.February(2021), false),
            new RequestsPatchRequestDailyData(3.February(2021), true),
            new RequestsPatchRequestDailyData(3.February(2021), false),
        ]);

        var controller = new RequestsController(
            CreateDateCalculator.WithActiveDates(activeDates),
            mockRequestRepository.Object,
            Mock.Of<ITriggerRepository>(),
            CreateUserRepository.WithUsers(DefaultUsers))
        {
            ControllerContext = CreateControllerContext.WithUsername(UserId)
        };

        await controller.PatchAsync(patchRequest);

        var expectedSavedRequests = new[]
        {
            new Request(UserId, 2.February(2021), RequestStatus.Pending),
            new Request(UserId, 3.February(2021), RequestStatus.Cancelled),
        };

        CheckSavedRequests(mockRequestRepository, expectedSavedRequests);
    }

    [Fact]
    public static async Task Does_not_update_unchanged_requests()
    {
        var activeDates = new[] { 2.February(2021), 3.February(2021) };

        var mockRequestRepository = new RequestRepositoryBuilder()
            .WithGetRequests(UserId, activeDates.ToDateInterval(), [])
            .BuildMock();

        var patchRequest = new RequestsPatchRequest(
        [
            new RequestsPatchRequestDailyData(2.February(2021), true),
            new RequestsPatchRequestDailyData(2.February(2021), false),
            new RequestsPatchRequestDailyData(3.February(2021), false),
            new RequestsPatchRequestDailyData(3.February(2021), true),
        ]);

        var controller = new RequestsController(
            CreateDateCalculator.WithActiveDates(activeDates),
            mockRequestRepository.Object,
            Mock.Of<ITriggerRepository>(),
            CreateUserRepository.WithUsers(DefaultUsers))
        {
            ControllerContext = CreateControllerContext.WithUsername(UserId)
        };

        await controller.PatchAsync(patchRequest);

        CheckSavedRequests(mockRequestRepository, []);
    }

    [Fact]
    public static async Task Does_not_update_requests_outside_active_date_range()
    {
        var activeDates = new[] { 2.February(2021), 3.February(2021) };

        var mockRequestRepository = new RequestRepositoryBuilder()
            .WithGetRequests(UserId, activeDates.ToDateInterval(), [])
            .BuildMock();

        var patchRequest = new RequestsPatchRequest(
        [
            new RequestsPatchRequestDailyData(1.February(2021), true),
            new RequestsPatchRequestDailyData(4.February(2021), true)
        ]);

        var controller = new RequestsController(
            CreateDateCalculator.WithActiveDates(activeDates),
            mockRequestRepository.Object,
            Mock.Of<ITriggerRepository>(),
            CreateUserRepository.WithUsers(DefaultUsers))
        {
            ControllerContext = CreateControllerContext.WithUsername(UserId)
        };

        await controller.PatchAsync(patchRequest);

        CheckSavedRequests(mockRequestRepository, []);
    }

    [Fact]
    public static async Task Updates_data_for_given_user_when_specified()
    {
        var activeDates = new[] { 2.February(2021) };

        var mockRequestRepository = new RequestRepositoryBuilder()
            .WithGetRequests(UserId, activeDates.ToDateInterval(), [])
            .BuildMock();

        var patchRequest = new RequestsPatchRequest(
        [
            new RequestsPatchRequestDailyData(2.February(2021), true),
            new RequestsPatchRequestDailyData(3.February(2021), false),
        ]);

        var controller = new RequestsController(
            CreateDateCalculator.WithActiveDates(activeDates),
            mockRequestRepository.Object,
            Mock.Of<ITriggerRepository>(),
            CreateUserRepository.WithUserExistsAndUsers(UserId, true, DefaultUsers));

        await controller.PatchByIdAsync(UserId, patchRequest);

        var expectedSavedRequests = new[] { new Request(UserId, 2.February(2021), RequestStatus.Pending) };

        CheckSavedRequests(mockRequestRepository, expectedSavedRequests);
    }

    [Fact]
    public static async Task Returns_updated_requests_after_saving()
    {
        var activeDates = new[] { 2.February(2021), 3.February(2021) };

        var returnedRequests = new[] { new Request(UserId, 2.February(2021), RequestStatus.Pending) };

        var requestRepository = new RequestRepositoryBuilder()
            .WithGetRequests(UserId, activeDates.ToDateInterval(), returnedRequests)
            .Build();

        var patchRequest =
            new RequestsPatchRequest([new RequestsPatchRequestDailyData(2.February(2021), true)]);

        var controller = new RequestsController(
            CreateDateCalculator.WithActiveDates(activeDates),
            requestRepository,
            Mock.Of<ITriggerRepository>(),
            CreateUserRepository.WithUsers(DefaultUsers))
        {
            ControllerContext = CreateControllerContext.WithUsername(UserId)
        };

        var result = await controller.PatchAsync(patchRequest);

        var resultValue = GetResultValue<RequestsResponse>(result);

        var day1data = GetDailyData(resultValue.Requests, 2.February(2021));
        var day2data = GetDailyData(resultValue.Requests, 3.February(2021));

        Assert.True(day1data.Requested);
        Assert.False(day2data.Requested);
    }

    [Fact]
    public static async Task Returns_404_response_when_given_user_to_update_does_not_exist()
    {
        var controller = new RequestsController(
            Mock.Of<IDateCalculator>(),
            Mock.Of<IRequestRepository>(),
            Mock.Of<ITriggerRepository>(),
            CreateUserRepository.WithUserExists(UserId, false));

        var result = await controller.PatchByIdAsync(
            UserId,
            new RequestsPatchRequest(Enumerable.Empty<RequestsPatchRequestDailyData>()));

        Assert.IsType<NotFoundResult>(result);
    }

    private static void CheckSavedRequests(
        Mock<IRequestRepository> mockRequestRepository,
        IReadOnlyCollection<Request> expectedSavedRequests)
    {
        mockRequestRepository.Verify(
            r => r.SaveRequests(
                It.Is<IReadOnlyCollection<Request>>(actual => CheckRequests(expectedSavedRequests, actual.ToList()))),
            Times.Once);
    }

    private static bool CheckRequests(
        IReadOnlyCollection<Request> expected,
        IReadOnlyCollection<Request> actual) =>
        actual.Count == expected.Count && expected.All(e => actual.Contains(e, new RequestsComparer()));
}