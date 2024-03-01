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
public class DailyDetailsController : ControllerBase
{
    private readonly IDateCalculator dateCalculator;
    private readonly IRequestRepository requestRepository;
    private readonly ITriggerRepository triggerRepository;
    private readonly IUserRepository userRepository;

    private static readonly IReadOnlyCollection<RequestStatus> UpdateableStatuses = new[]
    {
        RequestStatus.SoftInterrupted,
        RequestStatus.HardInterrupted
    };

    public DailyDetailsController(
        IDateCalculator dateCalculator,
        IRequestRepository requestRepository,
        ITriggerRepository triggerRepository,
        IUserRepository userRepository)
    {
        this.dateCalculator = dateCalculator;
        this.requestRepository = requestRepository;
        this.triggerRepository = triggerRepository;
        this.userRepository = userRepository;
    }

    [HttpGet("/DailyDetails")]
    [ProducesResponseType(typeof(DailyDetailsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync()
    {
        var activeDates = this.dateCalculator.GetActiveDates();

        var requests = await this.requestRepository.GetRequests(activeDates.ToDateInterval());

        var users = await this.userRepository.GetUsers();

        var data = activeDates
            .Select(d => CreateDailyData(d, this.GetCognitoUserId(), requests, users))
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
        var requests = await this.requestRepository.GetRequests(
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

        await this.requestRepository.SaveRequests(new[] { updatedRequest });

        await this.triggerRepository.AddTrigger();

        return await this.GetAsync();
    }

    private static Day<DailyDetailsData> CreateDailyData(
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
        var pendingRequests = filteredRequests
            .Where(r => r.Status == RequestStatus.Pending);

        var stayInterruptedStatus = CreateStayInterruptedStatus(currentUserId, filteredRequests);

        var data = new DailyDetailsData(
            allocatedUsers: CreateDailyDetailUsers(currentUserId, allocatedRequests, users),
            interruptedUsers: CreateDailyDetailUsers(currentUserId, interruptedRequests, users),
            pendingUsers: CreateDailyDetailUsers(currentUserId, pendingRequests, users),
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