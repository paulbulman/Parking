namespace Parking.Api.Json.DailyDetails;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class DailyDetailsData(
    IEnumerable<DailyDetailsUser> allocatedUsers,
    IEnumerable<DailyDetailsUser> interruptedUsers,
    IEnumerable<DailyDetailsUser> pendingUsers,
    StayInterruptedStatus stayInterruptedStatus)
{
    [Required]
    public IEnumerable<DailyDetailsUser> AllocatedUsers { get; } = allocatedUsers;

    [Required]
    public IEnumerable<DailyDetailsUser> InterruptedUsers { get; } = interruptedUsers;

    [Required]
    public IEnumerable<DailyDetailsUser> PendingUsers { get; } = pendingUsers;

    [Required]
    public StayInterruptedStatus StayInterruptedStatus { get; } = stayInterruptedStatus;
}