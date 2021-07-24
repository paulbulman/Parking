namespace Parking.Api.Controllers
{
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
        private readonly IUserRepository userRepository;

        public DailyDetailsController(
            IDateCalculator dateCalculator,
            IRequestRepository requestRepository,
            IUserRepository userRepository)
        {
            this.dateCalculator = dateCalculator;
            this.requestRepository = requestRepository;
            this.userRepository = userRepository;
        }

        [HttpGet]
        [ProducesResponseType(typeof(DailyDetailsResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAsync()
        {
            var activeDates = this.dateCalculator.GetActiveDates();

            var requests = await this.requestRepository.GetRequests(activeDates.First(), activeDates.Last());

            var users = await this.userRepository.GetUsers();

            var data = activeDates
                .Select(d => CreateDailyData(d, this.GetCognitoUserId(), requests, users))
                .ToArray();

            var response = new DailyDetailsResponse(data);

            return this.Ok(response);
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

            var interruptedStatuses = new[]
            {
                RequestStatus.Interrupted,
                RequestStatus.HardInterrupted,
                RequestStatus.SoftInterrupted
            };

            var allocatedRequests = filteredRequests
                .Where(r => r.Status == RequestStatus.Allocated);
            var interruptedRequests = filteredRequests
                .Where(r => interruptedStatuses.Contains(r.Status));
            var pendingRequests = filteredRequests
                .Where(r => r.Status == RequestStatus.Pending);

            var data = new DailyDetailsData(
                allocatedUsers: CreateDailyDetailUsers(currentUserId, allocatedRequests, users),
                interruptedUsers: CreateDailyDetailUsers(currentUserId, interruptedRequests, users),
                pendingUsers: CreateDailyDetailUsers(currentUserId, pendingRequests, users));

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
    }
}