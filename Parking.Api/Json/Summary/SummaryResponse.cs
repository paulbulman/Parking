using Parking.Api.Json.Calendar;

namespace Parking.Api.Json.Summary;

using System.ComponentModel.DataAnnotations;

public class SummaryResponse(Calendar<SummaryData> summary)
{
    [Required]
    public Calendar<SummaryData> Summary { get; } = summary;
}