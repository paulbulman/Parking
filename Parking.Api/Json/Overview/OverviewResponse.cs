namespace Parking.Api.Json.Overview;

using System.ComponentModel.DataAnnotations;
using Calendar;

public class OverviewResponse(Calendar<OverviewData> overview)
{
    [Required]
    public Calendar<OverviewData> Overview { get; } = overview;
}