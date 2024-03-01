namespace Parking.Api.Controllers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Business;
using Business.Data;
using Json.History;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Model;
using NodaTime;
using static Json.Calendar.Helpers;

[Route("[controller]")]
[ApiController]
public class HistoryController(
    IDateCalculator dateCalculator,
    IRequestRepository requestRepository,
    IReservationRepository reservationRepository)
    : Controller
{
    [HttpGet]
    [ProducesResponseType(typeof(HistoryResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync([FromQuery] string userId, [FromQuery] LocalDate lastDate)
    {
        var firstDate = lastDate.PlusDays(-60);

        var dateInterval = new DateInterval(firstDate, lastDate);

        var allRequests = await requestRepository.GetRequests(dateInterval);
        var reservations = await reservationRepository.GetReservations(dateInterval);

        var captions = dateInterval
            .Where(dateCalculator.IsWorkingDay)
            .ToDictionary(d => d, d => GetCaption(d, userId, allRequests, reservations));

        var allUsersInterruptionDates = allRequests
            .Where(r => r.Status.IsInterrupted())
            .Select(r => r.Date)
            .Distinct()
            .ToArray();

        var history = CreateCalendar(captions);

        var contestedRequests = allRequests.Where(r =>
                r.UserId == userId &&
                allUsersInterruptionDates.Contains(r.Date) &&
                !UserHasReservation(r, reservations))
            .ToArray();

        var allocatedContestedRequestsCount = contestedRequests.Count(
            r => r.Status == RequestStatus.Allocated);

        var totalContestedRequestsCount = contestedRequests.Count(r => r.Status.IsRequested());

        var allocationRatio = totalContestedRequestsCount == 0
            ? 0
            : (decimal)allocatedContestedRequestsCount / totalContestedRequestsCount;

        var response = new HistoryResponse(
            history,
            allocatedContestedRequestsCount: allocatedContestedRequestsCount,
            totalContestedRequestsCount: totalContestedRequestsCount,
            allocationRatio: allocationRatio);

        return this.Ok(response);
    }

    private static bool UserHasReservation(Request request, IEnumerable<Reservation> reservations) =>
        reservations.Any(r => r.UserId == request.UserId && r.Date == request.Date);

    private static string GetCaption(
        LocalDate localDate,
        string userId,
        IReadOnlyCollection<Request> allRequests,
        IReadOnlyCollection<Reservation> reservations)
    {
        var userRequest = allRequests.SingleOrDefault(r => r.Date == localDate && r.UserId == userId);

        return userRequest?.Status switch
        {
            null => string.Empty,
            RequestStatus.Allocated => GetAllocatedCaption(localDate, userId, allRequests, reservations),
            RequestStatus.Cancelled => "Cancelled",
            RequestStatus.HardInterrupted => "Interrupted (stay interrupted)",
            RequestStatus.SoftInterrupted => "Interrupted (day ahead)",
            RequestStatus.Interrupted => "Interrupted",
            RequestStatus.Pending => "Pending",
            _ => throw new ArgumentOutOfRangeException(nameof(userRequest.Status))
        };
    }

    private static string GetAllocatedCaption(
        LocalDate localDate,
        string userId,
        IReadOnlyCollection<Request> allRequests,
        IReadOnlyCollection<Reservation> reservations)
    {
        var hasReservation = reservations.Any(r => r.Date == localDate && r.UserId == userId);

        if (hasReservation)
        {
            return "Allocated (reserved)";
        }

        var isContested = allRequests.Any(r =>
            r.Date == localDate &&
            r.UserId != userId &&
            r.Status.IsInterrupted());

        return isContested ? "Allocated (contested)" : "Allocated (uncontested)";
    }
}