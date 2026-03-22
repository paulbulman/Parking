namespace Parking.Api.Controllers;

using System;
using System.Linq;
using System.Threading.Tasks;
using Business;
using Business.Data;
using Json.GuestRequests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Model;

[Authorize(Policy = "IsTeamLeader")]
[Route("guest-requests")]
[ApiController]
public class GuestRequestsController(
    IDateCalculator dateCalculator,
    IGuestRequestRepository guestRequestRepository,
    ITriggerRepository triggerRepository,
    IUserRepository userRepository)
    : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PostAsync([FromBody] GuestRequestsPostRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return this.BadRequest();
        }

        var activeDates = dateCalculator.GetActiveDates();

        if (!activeDates.Contains(request.Date))
        {
            return this.BadRequest();
        }

        var users = await userRepository.GetUsers();
        var visitingUser = users.SingleOrDefault(u => u.UserId == request.VisitingUserId);

        if (visitingUser == null)
        {
            return this.BadRequest();
        }

        var existingGuests = await guestRequestRepository.GetGuestRequests(request.Date.ToDateInterval());

        if (existingGuests.Any(g =>
            g.Date == request.Date &&
            string.Equals(g.Name, request.Name, StringComparison.OrdinalIgnoreCase)))
        {
            return this.Conflict();
        }

        var guestRequest = new GuestRequest(
            id: Guid.NewGuid().ToString(),
            date: request.Date,
            name: request.Name,
            visitingUserId: request.VisitingUserId,
            registrationNumber: request.RegistrationNumber,
            status: GuestRequestStatus.Pending);

        await guestRequestRepository.SaveGuestRequest(guestRequest);

        await triggerRepository.AddTrigger();

        return this.Ok();
    }
}
