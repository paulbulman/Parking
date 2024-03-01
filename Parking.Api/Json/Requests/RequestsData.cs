namespace Parking.Api.Json.Requests;

using System.ComponentModel.DataAnnotations;

public class RequestsData
{
    public RequestsData(bool requested) => this.Requested = requested;

    [Required]
    public bool Requested { get; }
}