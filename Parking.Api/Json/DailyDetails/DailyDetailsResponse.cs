namespace Parking.Api.Json.DailyDetails;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Calendar;

public class DailyDetailsResponse(IEnumerable<Day<DailyDetailsData>> details)
{
    [Required]
    public IEnumerable<Day<DailyDetailsData>> Details { get; } = details;
}