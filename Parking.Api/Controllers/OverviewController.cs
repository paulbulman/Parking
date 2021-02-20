
namespace Parking.Api.Controllers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Business;
    using Business.Data;
    using Json.Overview;
    using Microsoft.AspNetCore.Mvc;
    using Model;
    using NodaTime;
    using static Json.Calendar.Helpers;

    [Route("[controller]")]
    [ApiController]
    public class OverviewController : ControllerBase
    {
        private readonly IDateCalculator dateCalculator;
        private readonly IRequestRepository requestRepository;
        private readonly IUserRepository userRepository;

        public OverviewController(
            IDateCalculator dateCalculator,
            IRequestRepository requestRepository,
            IUserRepository userRepository)
        {
            this.dateCalculator = dateCalculator;
            this.requestRepository = requestRepository;
            this.userRepository = userRepository;
        }

        public async Task<IActionResult> GetAsync()
        {
            var activeDates = this.dateCalculator.GetActiveDates();

            var requests = await this.requestRepository.GetRequests(activeDates.First(), activeDates.Last());

            var users = await this.userRepository.GetUsers();

            var data = activeDates.ToDictionary(
                d => d,
                d => CreateDailyData(d, this.GetCognitoUserId(), requests, users));

            var calendar = CreateCalendar(data);

            return this.Ok(calendar);
        }

        private static OverviewData CreateDailyData(
            LocalDate localDate, 
            string currentUserId,
            IReadOnlyCollection<Request> requests,
            IReadOnlyCollection<User> users)
        {
            var filteredRequests = requests
                .Where(r => r.Date == localDate)
                .ToArray();

            var allocatedRequests = filteredRequests
                .Where(r => r.Status == RequestStatus.Allocated);
            var interruptedRequests = filteredRequests
                .Where(r => r.Status == RequestStatus.Requested);

            return new OverviewData(
                CreateOverviewUsers(currentUserId, allocatedRequests, users),
                CreateOverviewUsers(currentUserId, interruptedRequests, users));
        }

        private static IEnumerable<OverviewUser> CreateOverviewUsers(
            string currentUserId,
            IEnumerable<Request> requests,
            IEnumerable<User> users) =>
            requests
                .Select(r => users.Single(u => u.UserId == r.UserId))
                .OrderForDisplay()
                .Select(u => CreateOverviewUser(currentUserId, u));

        private static OverviewUser CreateOverviewUser(string currentUserId, User user) =>
            new OverviewUser(name: user.DisplayName(), isHighlighted: user.UserId == currentUserId);
    }
}