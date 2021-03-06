﻿namespace Parking.Api.Controllers
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using Business.Data;
    using Json.RegistrationNumbers;
    using Microsoft.AspNetCore.Mvc;
    using Model;

    [Route("[controller]")]
    [ApiController]
    public class RegistrationNumbersController : ControllerBase
    {
        private readonly IUserRepository userRepository;

        public RegistrationNumbersController(IUserRepository userRepository) => this.userRepository = userRepository;

        [HttpGet]
        public async Task<IActionResult> GetAsync()
        {
            var users = await this.userRepository.GetUsers();

            var data = users
                .Select(CreateRegistrationNumbersData)
                .SelectMany(d => d)
                .Where(d => !string.IsNullOrEmpty(d.RegistrationNumber))
                .OrderBy(d => d.RegistrationNumber)
                .ToArray();

            var response = new RegistrationNumbersResponse(data);

            return this.Ok(response);
        }

        private static IEnumerable<RegistrationNumbersData> CreateRegistrationNumbersData(User user) =>
            new[]
            {
                new RegistrationNumbersData(
                    FormatRegistrationNumber(user.RegistrationNumber),
                    user.DisplayName()),
                new RegistrationNumbersData(
                    FormatRegistrationNumber(user.AlternativeRegistrationNumber),
                    user.DisplayName())
            };

        private static string FormatRegistrationNumber(string rawRegistrationNumber) =>
            string.IsNullOrEmpty(rawRegistrationNumber)
                ? null
                : rawRegistrationNumber
                    .Replace(" ", string.Empty)
                    .ToUpper(CultureInfo.InvariantCulture);
    }
}