namespace Parking.Api.Json.Overview;

using System.ComponentModel.DataAnnotations;
using Calendar;

public class OverviewResponse
{
    public OverviewResponse(Calendar<OverviewData> overview) => this.Overview = overview;

    [Required]
    public Calendar<OverviewData> Overview { get; }
}