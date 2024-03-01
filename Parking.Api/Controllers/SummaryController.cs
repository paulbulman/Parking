namespace Parking.Api.Controllers;

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

[Route("[controller]")]
[ApiController]
public class SummaryController(
    IDateCalculator dateCalculator,
    IRequestRepository requestRepository)
    : Controller
{
    [HttpGet]
    [ProducesResponseType(typeof(SummaryResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummaryAsync()
    {
        var activeDates = dateCalculator.GetActiveDates();

        var requests =
            await requestRepository.GetRequests(this.GetCognitoUserId(), activeDates.ToDateInterval());

        var data = activeDates.ToDictionary(
            d => d,
            d => CreateDailyData(d, requests));

        var calendar = CreateCalendar(data);

        var response = new SummaryResponse(calendar);

        return this.Ok(response);
    }

    private static SummaryData CreateDailyData(
        LocalDate localDate,
        IReadOnlyCollection<Request> requests)
    {
        var requestStatus = requests.SingleOrDefault(r => r.Date == localDate)?.Status;
            
        var summaryStatus = GetSummaryStatus(requestStatus);

        var isProblem = 
            summaryStatus == SummaryStatus.Interrupted ||
            summaryStatus == SummaryStatus.HardInterrupted;

        return new SummaryData(summaryStatus, isProblem);
    }

    private static SummaryStatus? GetSummaryStatus(RequestStatus? requestStatus) =>
        requestStatus switch
        {
            RequestStatus.Allocated => SummaryStatus.Allocated,
            RequestStatus.Cancelled => null,
            RequestStatus.HardInterrupted => SummaryStatus.HardInterrupted,
            RequestStatus.Interrupted => SummaryStatus.Interrupted,
            RequestStatus.Pending => SummaryStatus.Pending,
            RequestStatus.SoftInterrupted => SummaryStatus.Interrupted,
            null => null,
            _ => throw new ArgumentOutOfRangeException(nameof(requestStatus), requestStatus, null)
        };
}