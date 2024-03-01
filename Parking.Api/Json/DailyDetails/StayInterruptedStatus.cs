namespace Parking.Api.Json.DailyDetails;

using System.ComponentModel.DataAnnotations;

public class StayInterruptedStatus(bool isAllowed, bool isSet)
{
    [Required]
    public bool IsAllowed { get; } = isAllowed;

    [Required]
    public bool IsSet { get; } = isSet;
}