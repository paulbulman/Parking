namespace Parking.Api.Controllers;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Business.Data;
using Json.RegistrationNumbers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Model;

[Route("[controller]")]
[ApiController]
public class RegistrationNumbersController(IUserRepository userRepository) : ControllerBase
{
    [HttpGet("{searchString}")]
    [ProducesResponseType(typeof(RegistrationNumbersResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(string searchString)
    {
        var users = await userRepository.GetUsers();

        var data = users
            .Select(CreateRegistrationNumbersData)
            .SelectMany(d => d)
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