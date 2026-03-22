namespace Parking.Api.Controllers;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Business;
using Business.Data;
using Json.Calendar;
using Json.DailyDetails;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Model;
using NodaTime;

[Route("[controller]")]
[ApiController]
public class DailyDetailsController(
    IDateCalculator dateCalculator,
    IGuestRequestRepository guestRequestRepository,
    IRequestRepository requestRepository,
    ITriggerRepository triggerRepository,
    IUserRepository userRepository)
    : ControllerBase
{
    private static readonly IReadOnlyCollection<RequestStatus> UpdateableStatuses =
    [
        RequestStatus.SoftInterrupted,
        RequestStatus.HardInterrupted
    ];

    [HttpGet("/DailyDetails")]
    [ProducesResponseType(typeof(DailyDetailsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync()
    {
        var activeDates = dateCalculator.GetActiveDates();

        var requests = await requestRepository.GetRequests(activeDates.ToDateInterval());

        var guestRequests = await guestRequestRepository.GetGuestRequests(activeDates.ToDateInterval());

        var users = await userRepository.GetUsers();

        var data = activeDates
            .Select(d => CreateDailyData(d, this.GetCognitoUserId(), requests, guestRequests, users))
            .ToArray();

        var response = new DailyDetailsResponse(data);

        return this.Ok(response);
    }

    [HttpPatch("/StayInterrupted")]
    [ProducesResponseType(typeof(DailyDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PatchAsync([FromBody] StayInterruptedPatchRequest patchRequest)
    {
        var requests = await requestRepository.GetRequests(
            this.GetCognitoUserId(),
            patchRequest.LocalDate.ToDateInterval());

        var request = requests.SingleOrDefault();

        if (request == null)
        {
            return this.NotFound();
        }

        if (!UpdateableStatuses.Contains(request.Status))
        {
            return this.BadRequest();
        }

        var updatedRequestStatus = patchRequest.StayInterrupted
            ? RequestStatus.HardInterrupted
            : RequestStatus.SoftInterrupted;

        var updatedRequest = new Request(request.UserId, request.Date, updatedRequestStatus);

        await requestRepository.SaveRequests([updatedRequest]);

        await triggerRepository.AddTrigger();

        return await this.GetAsync();
    }

    private static Day<DailyDetailsData> CreateDailyData(
        LocalDate localDate,
        string currentUserId,
        IReadOnlyCollection<Request> requests,
        IReadOnlyCollection<GuestRequest> guestRequests,
        IReadOnlyCollection<User> users)
    {
        var filteredRequests = requests
            .Where(r => r.Date == localDate)
            .ToArray();

        var filteredGuests = guestRequests
            .Where(g => g.Date == localDate)
            .ToArray();

        var userLookup = users.ToDictionary(u => u.UserId);

        var allocatedRequests = filteredRequests
            .Where(r => r.Status == RequestStatus.Allocated);
        var interruptedRequests = filteredRequests
            .Where(r => r.Status.IsInterrupted());
        var pendingRequests = filteredRequests
            .Where(r => r.Status == RequestStatus.Pending);

        var allocatedUsers = CreateDailyDetailUsers(currentUserId, allocatedRequests, users)
            .Concat(filteredGuests
                .Where(g => g.Status == GuestRequestStatus.Allocated)
                .Select(g => new DailyDetailsUser(
                    name: g.FormatGuestName(userLookup),
                    isHighlighted: false)));

        var interruptedUsers = CreateDailyDetailUsers(currentUserId, interruptedRequests, users)
            .Concat(filteredGuests
                .Where(g => g.Status == GuestRequestStatus.Interrupted)
                .Select(g => new DailyDetailsUser(
                    name: g.FormatGuestName(userLookup),
                    isHighlighted: false)));

        var pendingUsers = CreateDailyDetailUsers(currentUserId, pendingRequests, users)
            .Concat(filteredGuests
                .Where(g => g.Status == GuestRequestStatus.Pending)
                .Select(g => new DailyDetailsUser(
                    name: g.FormatGuestName(userLookup),
                    isHighlighted: false)));

        var stayInterruptedStatus = CreateStayInterruptedStatus(currentUserId, filteredRequests);

        var data = new DailyDetailsData(
            allocatedUsers: allocatedUsers,
            interruptedUsers: interruptedUsers,
            pendingUsers: pendingUsers,
            stayInterruptedStatus: stayInterruptedStatus);

        return new Day<DailyDetailsData>(localDate, data);
    }

    private static IEnumerable<DailyDetailsUser> CreateDailyDetailUsers(
        string currentUserId,
        IEnumerable<Request> requests,
        IEnumerable<User> users) =>
        requests
            .Select(r => users.Single(u => u.UserId == r.UserId))
            .OrderForDisplay()
            .Select(u => CreateDailyDetailsUser(currentUserId, u));

    private static DailyDetailsUser CreateDailyDetailsUser(string currentUserId, User user) =>
        new DailyDetailsUser(name: user.DisplayName(), isHighlighted: user.UserId == currentUserId);

    private static StayInterruptedStatus CreateStayInterruptedStatus(
        string currentUserId,
        IEnumerable<Request> requests)
    {
        var currentUserRequestStatus = requests.SingleOrDefault(r => r.UserId == currentUserId)?.Status;

        var isAllowed = currentUserRequestStatus.HasValue && UpdateableStatuses.Contains(currentUserRequestStatus.Value);
        var isSet = currentUserRequestStatus == RequestStatus.HardInterrupted;

        return new StayInterruptedStatus(isAllowed: isAllowed, isSet: isSet);
    }
}
