using Parking.Api.Json.Calendar;

namespace Parking.Api.Json.Summary;

using System.ComponentModel.DataAnnotations;

public class SummaryResponse
{
    public SummaryResponse(Calendar<SummaryData> summary) => this.Summary = summary;

    [Required]
    public Calendar<SummaryData> Summary { get; }
}