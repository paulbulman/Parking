namespace Parking.Api.Json.Requests;

using System.ComponentModel.DataAnnotations;
using Calendar;

public class RequestsResponse(Calendar<RequestsData> requests)
{
    [Required]
    public Calendar<RequestsData> Requests { get; } = requests;
}