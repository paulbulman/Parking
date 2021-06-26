namespace Parking.Api.Controllers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Business;
    using Business.Data;
    using Json.Summary;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Model;
    using NodaTime;
    using static Json.Calendar.Helpers;

    [Route("[controller]")]
    [ApiController]
    public class SummaryController : Controller
    {
        private readonly IDateCalculator dateCalculator;
        private readonly IRequestRepository requestRepository;

        public SummaryController(IDateCalculator dateCalculator, IRequestRepository requestRepository)
        {
            this.dateCalculator = dateCalculator;
            this.requestRepository = requestRepository;
        }

        [HttpGet]
        [ProducesResponseType(typeof(SummaryResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAsync()
        {
            var activeDates = this.dateCalculator.GetActiveDates();
            var lastLongLeadTimeAllocationDate = this.dateCalculator.GetLongLeadTimeAllocationDates().Last();

            var requests = await this.requestRepository.GetRequests(
                this.GetCognitoUserId(),
                activeDates.First(),
                activeDates.Last());

            var data = activeDates.ToDictionary(
                d => d,
                d => CreateDailyData(d, requests, lastLongLeadTimeAllocationDate));

            var calendar = CreateCalendar(data);

            var response = new SummaryResponse(calendar);

            return this.Ok(response);
        }

        private static SummaryData CreateDailyData(
            LocalDate localDate,
            IReadOnlyCollection<Request> requests,
            LocalDate lastLongLeadTimeAllocationDate)
        {
            var matchingRequest = requests.SingleOrDefault(r => r.Date == localDate);

            if (matchingRequest == null || matchingRequest.Status == RequestStatus.Cancelled)
            {
                return new SummaryData(null, isProblem: false);
            }

            if (matchingRequest.Status == RequestStatus.Allocated)
            {
                return new SummaryData(SummaryStatus.Allocated, isProblem: false);
            }

            return matchingRequest.Date <= lastLongLeadTimeAllocationDate
                ? new SummaryData(SummaryStatus.Interrupted, isProblem: true)
                : new SummaryData(SummaryStatus.Requested, isProblem: false);
        }
    }
}