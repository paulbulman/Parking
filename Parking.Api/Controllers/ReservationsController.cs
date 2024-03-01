namespace Parking.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Business;
using Business.Data;
using Json.Reservations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Model;
using NodaTime;
using static Json.Calendar.Helpers;

[Authorize(Policy = "IsTeamLeader")]
[Route("[controller]")]
[ApiController]
public class ReservationsController(
    IConfigurationRepository configurationRepository,
    IDateCalculator dateCalculator,
    IReservationRepository reservationRepository,
    IUserRepository userRepository)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ReservationsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync()
    {
        var response = await this.GetReservations();

        return this.Ok(response);
    }

    [HttpPatch]
    [ProducesResponseType(typeof(ReservationsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> PatchAsync([FromBody] ReservationsPatchRequest request)
    {
        var activeDates = dateCalculator.GetActiveDates();

        var reservations = request.Reservations
            .Where(r => activeDates.Contains(r.LocalDate))
            .SelectMany(CreateReservations)
            .ToList();

        await reservationRepository.SaveReservations(reservations);

        var response = await this.GetReservations();

        return this.Ok(response);
    }

    private async Task<ReservationsResponse> GetReservations()
    {
        var configuration = await configurationRepository.GetConfiguration();

        var activeDates = dateCalculator.GetActiveDates();

        var reservations = await reservationRepository.GetReservations(activeDates.ToDateInterval());

        var calendarData = activeDates.ToDictionary(
            d => d,
            d => CreateDailyData(d, reservations));

        var calendar = CreateCalendar(calendarData);

        var users = await userRepository.GetUsers();

        var reservationsUsers = users
            .OrderBy(u => u.LastName)
            .Select(u => new ReservationsUser(u.UserId, u.DisplayName()));

        return new ReservationsResponse(calendar, configuration.ShortLeadTimeSpaces, reservationsUsers);
    }

    private static ReservationsData CreateDailyData(
        LocalDate localDate,
        IReadOnlyCollection<Reservation> reservations)
    {
        var filteredReservations = reservations.Where(r => r.Date == localDate);

        return new ReservationsData(filteredReservations.Select(r => r.UserId));
    }

    private static IEnumerable<Reservation> CreateReservations(ReservationsPatchRequestDailyData dailyData) =>
        dailyData.UserIds.Select(userId => new Reservation(userId, dailyData.LocalDate));
}