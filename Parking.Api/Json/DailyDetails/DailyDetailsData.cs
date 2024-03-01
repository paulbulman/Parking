namespace Parking.Api.Json.DailyDetails;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class DailyDetailsData
{
    public DailyDetailsData(
        IEnumerable<DailyDetailsUser> allocatedUsers,
        IEnumerable<DailyDetailsUser> interruptedUsers,
        IEnumerable<DailyDetailsUser> pendingUsers,
        StayInterruptedStatus stayInterruptedStatus)
    {
        this.AllocatedUsers = allocatedUsers;
        this.InterruptedUsers = interruptedUsers;
        this.PendingUsers = pendingUsers;
        this.StayInterruptedStatus = stayInterruptedStatus;
    }

    [Required]
    public IEnumerable<DailyDetailsUser> AllocatedUsers { get; }
        
    [Required]
    public IEnumerable<DailyDetailsUser> InterruptedUsers { get; }
        
    [Required]
    public IEnumerable<DailyDetailsUser> PendingUsers { get; }

    [Required]
    public StayInterruptedStatus StayInterruptedStatus { get; }
}