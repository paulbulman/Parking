namespace Parking.Api.Json.DailyDetails;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Calendar;

public class DailyDetailsResponse
{
    public DailyDetailsResponse(IEnumerable<Day<DailyDetailsData>> details) =>
        this.Details = details;
        
    [Required]
    public IEnumerable<Day<DailyDetailsData>> Details { get; }
}