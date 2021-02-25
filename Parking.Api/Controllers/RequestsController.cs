﻿namespace Parking.Api.Controllers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Business;
    using Business.Data;
    using Json.Requests;
    using Microsoft.AspNetCore.Mvc;
    using Model;
    using NodaTime;
    using static Json.Calendar.Helpers;

    [Route("[controller]")]
    [ApiController]
    public class RequestsController : ControllerBase
    {
        private readonly IDateCalculator dateCalculator;
        
        private readonly IRequestRepository requestRepository;

        public RequestsController(
            IDateCalculator dateCalculator,
            IRequestRepository requestRepository)
        {
            this.dateCalculator = dateCalculator;
            this.requestRepository = requestRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync()
        {
            var userId = this.GetCognitoUserId();

            var activeDates = this.dateCalculator.GetActiveDates();

            var requests = await this.requestRepository.GetRequests(userId, activeDates.First(), activeDates.Last());

            var data = activeDates.ToDictionary(
                d => d,
                d => CreateDailyData(d, requests));

            var calendar = CreateCalendar(data);

            var response = new RequestsResponse(calendar);

            return this.Ok(response);
        }

        private static RequestsData CreateDailyData(LocalDate localDate, IReadOnlyCollection<Request> requests)
        {
            var requestedStatuses = new[] {RequestStatus.Allocated, RequestStatus.Requested};

            var matchingRequest = requests.SingleOrDefault(r => r.Date == localDate);

            var requested = matchingRequest != null && requestedStatuses.Contains(matchingRequest.Status);

            return new RequestsData(requested);
        }
    }
}
