namespace Parking.Api.Controllers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Business;
using Business.Data;
using Json.GuestRequests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Model;
using NodaTime;
using NodaTime.Text;

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
    [HttpGet]
    [ProducesResponseType(typeof(GuestRequestsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync()
    {
        var activeDates = dateCalculator.GetActiveDates();
        var firstDate = activeDates.First().PlusDays(-60);
        var lastDate = activeDates.Last();
        var dateInterval = new DateInterval(firstDate, lastDate);

        var guestRequests = await guestRequestRepository.GetGuestRequests(dateInterval);

        var users = await userRepository.GetUsers();
        var userLookup = users.ToDictionary(u => u.UserId);

        var data = guestRequests
            .Where(g => dateInterval.Contains(g.Date))
            .OrderBy(g => g.Date)
            .ThenBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => new GuestRequestsData(
                id: g.Id,
                date: LocalDatePattern.Iso.Format(g.Date),
                name: g.Name,
                visitingUserId: g.VisitingUserId,
                visitingUserDisplayName: GetVisitingUserDisplayName(userLookup, g.VisitingUserId),
                registrationNumber: g.RegistrationNumber,
                status: g.Status))
            .ToArray();

        return this.Ok(new GuestRequestsResponse(data));
    }

    private static string GetVisitingUserDisplayName(
        IReadOnlyDictionary<string, User> userLookup,
        string visitingUserId) =>
        userLookup.TryGetValue(visitingUserId, out var user)
            ? user.DisplayName()
            : "deleted user";

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

    [HttpPut("{date}/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PutAsync(string date, string id, [FromBody] GuestRequestsPutRequest request)
    {
        var localDate = LocalDatePattern.Iso.Parse(date);

        if (!localDate.Success)
        {
            return this.BadRequest();
        }

        var existingGuests = await guestRequestRepository.GetGuestRequests(localDate.Value.ToDateInterval());
        var existingGuest = existingGuests.SingleOrDefault(g => g.Id == id);

        if (existingGuest == null)
        {
            return this.NotFound();
        }

        var users = await userRepository.GetUsers();
        var visitingUser = users.SingleOrDefault(u => u.UserId == request.VisitingUserId);

        if (visitingUser == null)
        {
            return this.BadRequest();
        }

        if (!string.Equals(existingGuest.Name, request.Name, StringComparison.OrdinalIgnoreCase) &&
            existingGuests.Any(g =>
                string.Equals(g.Name, request.Name, StringComparison.OrdinalIgnoreCase)))
        {
            return this.Conflict();
        }

        var updatedGuestRequest = new GuestRequest(
            id: existingGuest.Id,
            date: existingGuest.Date,
            name: request.Name,
            visitingUserId: request.VisitingUserId,
            registrationNumber: request.RegistrationNumber,
            status: existingGuest.Status);

        await guestRequestRepository.UpdateGuestRequest(updatedGuestRequest);

        return this.Ok();
    }

    [HttpDelete("{date}/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(string date, string id)
    {
        var localDate = LocalDatePattern.Iso.Parse(date);

        if (!localDate.Success)
        {
            return this.BadRequest();
        }

        var existingGuests = await guestRequestRepository.GetGuestRequests(localDate.Value.ToDateInterval());
        var existingGuest = existingGuests.SingleOrDefault(g => g.Id == id);

        if (existingGuest == null)
        {
            return this.NotFound();
        }

        await guestRequestRepository.DeleteGuestRequest(localDate.Value, id);

        await triggerRepository.AddTrigger();

        return this.Ok();
    }
}
