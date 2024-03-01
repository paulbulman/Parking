namespace Parking.Api.Json.Requests;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class RequestsPatchRequest(IEnumerable<RequestsPatchRequestDailyData> requests)
{
    [Required]
    public IEnumerable<RequestsPatchRequestDailyData> Requests { get; } = requests;
}