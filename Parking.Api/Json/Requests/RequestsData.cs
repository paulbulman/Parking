namespace Parking.Api.Json.Requests;

using System.ComponentModel.DataAnnotations;

public class RequestsData(bool requested)
{
    [Required]
    public bool Requested { get; } = requested;
}