namespace Parking.Api.Json.DailyDetails;

using System.ComponentModel.DataAnnotations;

public class StayInterruptedStatus
{
    public StayInterruptedStatus(bool isAllowed, bool isSet)
    {
        this.IsAllowed = isAllowed;
        this.IsSet = isSet;
    }

    [Required]
    public bool IsAllowed { get; }
        
    [Required]
    public bool IsSet { get; }
}