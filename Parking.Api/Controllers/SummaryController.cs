namespace Parking.Api.Controllers
{
    using System;
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

    [ApiController]
    public class SummaryController : Controller
    {
        private readonly IDateCalculator dateCalculator;
        private readonly IRequestRepository requestRepository;
        private readonly ITriggerRepository triggerRepository;

        private static readonly IReadOnlyCollection<RequestStatus> UpdateableStatuses = new[]
        {
            RequestStatus.SoftInterrupted,
            RequestStatus.HardInterrupted
        };

        public SummaryController(
            IDateCalculator dateCalculator,
            IRequestRepository requestRepository,
            ITriggerRepository triggerRepository)
        {
            this.dateCalculator = dateCalculator;
            this.requestRepository = requestRepository;
            this.triggerRepository = triggerRepository;
        }

        [HttpGet("/Summary")]
        [ProducesResponseType(typeof(SummaryResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSummaryAsync()
        {
            var activeDates = this.dateCalculator.GetActiveDates();

            var requests = await this.requestRepository.GetRequests(
                this.GetCognitoUserId(),
                activeDates.First(),
                activeDates.Last());

            var data = activeDates.ToDictionary(
                d => d,
                d => CreateDailyData(d, requests));

            var calendar = CreateCalendar(data);

            var stayInterruptedStatus = CreateStayInterruptedStatus(
                activeDates.First(),
                requests.FirstOrDefault()?.Status);

            var response = new SummaryResponse(calendar, stayInterruptedStatus);

            return this.Ok(response);
        }

        [HttpPatch("/StayInterrupted")]
        [ProducesResponseType(typeof(SummaryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateStayInterruptedAsync([FromBody] StayInterruptedPatchRequest patchRequest)
        {
            var requests = await this.requestRepository.GetRequests(
                this.GetCognitoUserId(),
                patchRequest.LocalDate,
                patchRequest.LocalDate);

            var request = requests.SingleOrDefault();

            if (request == null)
            {
                return this.NotFound();
            }

            if (!UpdateableStatuses.Contains(request.Status))
            {
                return this.BadRequest();
            }

            var updatedRequestStatus = patchRequest.StayInterrupted
                ? RequestStatus.HardInterrupted
                : RequestStatus.SoftInterrupted;

            var updatedRequest = new Request(request.UserId, request.Date, updatedRequestStatus);

            await this.requestRepository.SaveRequests(new[] { updatedRequest });

            await this.triggerRepository.AddTrigger();

            return await this.GetSummaryAsync();
        }

        private static SummaryData CreateDailyData(
            LocalDate localDate,
            IReadOnlyCollection<Request> requests)
        {
            var requestStatus = requests.SingleOrDefault(r => r.Date == localDate)?.Status;
            
            var summaryStatus = GetSummaryStatus(requestStatus);

            var isProblem = summaryStatus == SummaryStatus.Interrupted;

            return new SummaryData(summaryStatus, isProblem);
        }

        private static SummaryStatus? GetSummaryStatus(RequestStatus? requestStatus) =>
            requestStatus switch
            {
                RequestStatus.Allocated => SummaryStatus.Allocated,
                RequestStatus.Cancelled => null,
                RequestStatus.HardInterrupted => SummaryStatus.Interrupted,
                RequestStatus.Interrupted => SummaryStatus.Interrupted,
                RequestStatus.Pending => SummaryStatus.Pending,
                RequestStatus.SoftInterrupted => SummaryStatus.Interrupted,
                null => null,
                _ => throw new ArgumentOutOfRangeException(nameof(requestStatus), requestStatus, null)
            };

        private static StayInterruptedStatus CreateStayInterruptedStatus(
            LocalDate localDate, 
            RequestStatus? firstRequestStatus)
        {
            var isAllowed = firstRequestStatus.HasValue && UpdateableStatuses.Contains(firstRequestStatus.Value);
            var isSet = firstRequestStatus == RequestStatus.HardInterrupted;

            return new StayInterruptedStatus(localDate: localDate, isAllowed: isAllowed, isSet: isSet);
        }
    }
}