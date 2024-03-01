namespace Parking.Api.Json.Overview;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class OverviewData(IEnumerable<OverviewUser> allocatedUsers, IEnumerable<OverviewUser> interruptedUsers)
{
    [Required]
    public IEnumerable<OverviewUser> AllocatedUsers { get; } = allocatedUsers;

    [Required]
    public IEnumerable<OverviewUser> InterruptedUsers { get; } = interruptedUsers;
}