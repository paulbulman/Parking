namespace Parking.Api.Controllers;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Business;
using Business.Data;
using Json.RegistrationNumbers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Model;
using NodaTime;

[Route("[controller]")]
[ApiController]
public class RegistrationNumbersController(
    IDateCalculator dateCalculator,
    IGuestRequestRepository guestRequestRepository,
    IUserRepository userRepository) : ControllerBase
{
    [HttpGet("{searchString}")]
    [ProducesResponseType(typeof(RegistrationNumbersResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(string searchString)
    {
        var users = await userRepository.GetUsers();

        var userData = users
            .Select(CreateRegistrationNumbersData)
            .SelectMany(d => d);

        var activeDates = dateCalculator.GetActiveDates();
        var guestDateInterval = new DateInterval(activeDates.First().PlusDays(-60), activeDates.Last());

        var guestRequests = await guestRequestRepository.GetGuestRequests(guestDateInterval);

        var userLookup = users.ToDictionary(u => u.UserId);

        var guestData = guestRequests
            .Where(g => !string.IsNullOrEmpty(g.RegistrationNumber))
            .Select(g => new RegistrationNumbersData(
                FormatRegistrationNumber(g.RegistrationNumber!),
                g.FormatGuestName(userLookup)));

        var data = userData.Concat(guestData)
            .Where(d =>
                !string.IsNullOrEmpty(d.RegistrationNumber) &&
                NormalizeRegistrationNumber(d.RegistrationNumber) == NormalizeRegistrationNumber(searchString))
            .OrderBy(d => d.RegistrationNumber)
            .ToArray();

        var response = new RegistrationNumbersResponse(data);

        return this.Ok(response);
    }

    private static string NormalizeRegistrationNumber(string rawRegistrationNumber) =>
        Regex.Replace(rawRegistrationNumber, "[^a-zA-Z0-9]", string.Empty)
            .Replace("I", "1", StringComparison.OrdinalIgnoreCase)
            .Replace("L", "1", StringComparison.OrdinalIgnoreCase)
            .Replace("S", "5", StringComparison.OrdinalIgnoreCase)
            .Replace("O", "0", StringComparison.OrdinalIgnoreCase)
            .Replace("Z", "2", StringComparison.OrdinalIgnoreCase)
            .ToUpper(CultureInfo.InvariantCulture);

    private static IEnumerable<RegistrationNumbersData> CreateRegistrationNumbersData(User user) =>
        new[] { user.RegistrationNumber, user.AlternativeRegistrationNumber }
            .Where(r => !string.IsNullOrEmpty(r))
            .Select(r => FormatRegistrationNumber(r!))
            .Select(r => new RegistrationNumbersData(r, user.DisplayName()));

    private static string FormatRegistrationNumber(string rawRegistrationNumber) =>
        Regex.Replace(rawRegistrationNumber, "[^a-zA-Z0-9]", string.Empty)
            .ToUpper(CultureInfo.InvariantCulture);

}
