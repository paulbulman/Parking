﻿namespace Parking.Api.Controllers;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Business;
using Business.Data;
using Json.Overview;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Model;
using NodaTime;
using static Json.Calendar.Helpers;

[Route("[controller]")]
[ApiController]
public class OverviewController(
    IDateCalculator dateCalculator,
    IRequestRepository requestRepository,
    IUserRepository userRepository)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(OverviewResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync()
    {
        var activeDates = dateCalculator.GetActiveDates();

        var requests = await requestRepository.GetRequests(activeDates.ToDateInterval());

        var users = await userRepository.GetUsers();

        var data = activeDates.ToDictionary(
            d => d,
            d => CreateDailyData(d, this.GetCognitoUserId(), requests, users));

        var calendar = CreateCalendar(data);

        var response = new OverviewResponse(calendar);

        return this.Ok(response);
    }

    private static OverviewData CreateDailyData(
        LocalDate localDate,
        string currentUserId,
        IReadOnlyCollection<Request> requests,
        IReadOnlyCollection<User> users)
    {
        var filteredRequests = requests
            .Where(r => r.Date == localDate)
            .ToArray();

        var allocatedRequests = filteredRequests
            .Where(r => r.Status == RequestStatus.Allocated);
        var interruptedRequests = filteredRequests
            .Where(r => r.Status.IsInterrupted());

        return new OverviewData(
            CreateOverviewUsers(currentUserId, allocatedRequests, users),
            CreateOverviewUsers(currentUserId, interruptedRequests, users));
    }

    private static IEnumerable<OverviewUser> CreateOverviewUsers(
        string currentUserId,
        IEnumerable<Request> requests,
        IEnumerable<User> users) =>
        requests
            .Select(r => users.Single(u => u.UserId == r.UserId))
            .OrderForDisplay()
            .Select(u => CreateOverviewUser(currentUserId, u));

    private static OverviewUser CreateOverviewUser(string currentUserId, User user) =>
        new OverviewUser(name: user.DisplayName(), isHighlighted: user.UserId == currentUserId);
}