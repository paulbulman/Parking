namespace Parking.Api.Controllers;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Business;
using Business.Data;
using Json.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Model;
using NodaTime;
using static Json.Calendar.Helpers;

[Route("[controller]")]
[ApiController]
public class RequestsController(
    IDateCalculator dateCalculator,
    IRequestRepository requestRepository,
    ITriggerRepository triggerRepository,
    IUserRepository userRepository)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(RequestsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync()
    {
        var userId = this.GetCognitoUserId();

        var response = await this.GetRequests(userId);

        return this.Ok(response);
    }

    [Authorize(Policy = "IsTeamLeader")]
    [HttpGet("{userId}")]
    [ProducesResponseType(typeof(RequestsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(string userId)
    {
        if (!await userRepository.UserExists(userId))
        {
            return this.NotFound();
        }

        var response = await this.GetRequests(userId);

        return this.Ok(response);
    }

    [HttpPatch]
    [ProducesResponseType(typeof(RequestsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> PatchAsync([FromBody] RequestsPatchRequest request)
    {
        var userId = this.GetCognitoUserId();

        await this.UpdateRequests(userId, request);

        var response = await this.GetRequests(userId);

        return this.Ok(response);
    }

    [Authorize(Policy = "IsTeamLeader")]
    [HttpPatch("{userId}")]
    [ProducesResponseType(typeof(RequestsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PatchByIdAsync(string userId, [FromBody] RequestsPatchRequest request)
    {
        if (!await userRepository.UserExists(userId))
        {
            return this.NotFound();
        }

        await this.UpdateRequests(userId, request);

        var response = await this.GetRequests(userId);

        return this.Ok(response);
    }

    private async Task<RequestsResponse> GetRequests(string userId)
    {
        var activeDates = dateCalculator.GetActiveDates();

        var requests = await requestRepository.GetRequests(userId, activeDates.ToDateInterval());

        var data = activeDates.ToDictionary(
            d => d,
            d => CreateDailyData(d, requests));

        var calendar = CreateCalendar(data);

        return new RequestsResponse(calendar);
    }

    private static RequestsData CreateDailyData(LocalDate localDate, IReadOnlyCollection<Request> requests)
    {
        var matchingRequest = requests.SingleOrDefault(r => r.Date == localDate);

        var requested = matchingRequest != null && matchingRequest.Status.IsRequested();

        return new RequestsData(requested);
    }

    private async Task UpdateRequests(string userId, RequestsPatchRequest request)
    {
        var activeDates = dateCalculator.GetActiveDates();

        var requestsToSave = request.Requests
            .Where(r => activeDates.Contains(r.LocalDate))
            .GroupBy(r => r.LocalDate)
            .Where(g => !ValuesCancelOut(g))
            .Select(AuthoritativeValue)
            .Select(v => CreateRequest(userId, v))
            .ToArray();

        await requestRepository.SaveRequests(requestsToSave);

        await triggerRepository.AddTrigger();
    }

    private static bool ValuesCancelOut(
        IGrouping<LocalDate, RequestsPatchRequestDailyData> dailyRequests) => dailyRequests.Count() % 2 == 0;

    private static RequestsPatchRequestDailyData AuthoritativeValue(
        IGrouping<LocalDate, RequestsPatchRequestDailyData> dailyRequests) => dailyRequests.Last();

    private static Request CreateRequest(string userId, RequestsPatchRequestDailyData data)
    {
        var status = data.Requested ? RequestStatus.Pending : RequestStatus.Cancelled;

        return new Request(userId, data.LocalDate, status);
    }
}