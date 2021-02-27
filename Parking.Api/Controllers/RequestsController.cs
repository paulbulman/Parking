namespace Parking.Api.Controllers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Business;
    using Business.Data;
    using Json.Requests;
    using Microsoft.AspNetCore.Authorization;
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

        private readonly ITriggerRepository triggerRepository;

        private readonly IUserRepository userRepository;

        public RequestsController(
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

        [HttpGet]
        public async Task<IActionResult> GetAsync()
        {
            var userId = this.GetCognitoUserId();

            var response = await this.GetRequests(userId);

            return this.Ok(response);
        }

        [Authorize(Policy = "IsTeamLeader")]
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetAsync(string userId)
        {
            if (!await this.userRepository.UserExists(userId))
            {
                return this.NotFound();
            }

            var response = await this.GetRequests(userId);

            return this.Ok(response);
        }

        [HttpPatch]
        public async Task<IActionResult> PatchAsync([FromBody] RequestsPatchRequest request)
        {
            var userId = this.GetCognitoUserId();

            await this.UpdateRequests(userId, request);

            var response = await this.GetRequests(userId);

            return this.Ok(response);
        }

        [Authorize(Policy = "IsTeamLeader")]
        [HttpPatch("{userId}")]
        public async Task<IActionResult> PatchAsync(string userId, [FromBody] RequestsPatchRequest request)
        {
            if (!await this.userRepository.UserExists(userId))
            {
                return this.NotFound();
            }

            await this.UpdateRequests(userId, request);

            var response = await this.GetRequests(userId);

            return this.Ok(response);
        }

        private async Task<RequestsResponse> GetRequests(string userId)
        {
            var activeDates = this.dateCalculator.GetActiveDates();

            var requests = await this.requestRepository.GetRequests(userId, activeDates.First(), activeDates.Last());

            var data = activeDates.ToDictionary(
                d => d,
                d => CreateDailyData(d, requests));

            var calendar = CreateCalendar(data);

            return new RequestsResponse(calendar);
        }

        private static RequestsData CreateDailyData(LocalDate localDate, IReadOnlyCollection<Request> requests)
        {
            var requestedStatuses = new[] { RequestStatus.Allocated, RequestStatus.Requested };

            var matchingRequest = requests.SingleOrDefault(r => r.Date == localDate);

            var requested = matchingRequest != null && requestedStatuses.Contains(matchingRequest.Status);

            return new RequestsData(requested);
        }

        private async Task UpdateRequests(string userId, RequestsPatchRequest request)
        {
            var activeDates = this.dateCalculator.GetActiveDates();

            var requestsToSave = request.Requests
                .Where(r => activeDates.Contains(r.Date))
                .GroupBy(r => r.Date)
                .Where(g => !ValuesCancelOut(g))
                .Select(AuthoritativeValue)
                .Select(v => CreateRequest(userId, v))
                .ToArray();

            await this.requestRepository.SaveRequests(requestsToSave);

            await this.triggerRepository.AddTrigger();
        }

        private static bool ValuesCancelOut(
            IGrouping<LocalDate, RequestPatchRequestDailyData> dailyRequests) => dailyRequests.Count() % 2 == 0;

        private static RequestPatchRequestDailyData AuthoritativeValue(
            IGrouping<LocalDate, RequestPatchRequestDailyData> dailyRequests) => dailyRequests.Last();

        private static Request CreateRequest(string userId, RequestPatchRequestDailyData data)
        {
            var status = data.Requested ? RequestStatus.Requested : RequestStatus.Cancelled;

            return new Request(userId, data.Date, status);
        }
    }
}
