namespace Parking.Api.Json.Overview;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class OverviewData
{
    public OverviewData(IEnumerable<OverviewUser> allocatedUsers, IEnumerable<OverviewUser> interruptedUsers)
    {
        this.AllocatedUsers = allocatedUsers;
        this.InterruptedUsers = interruptedUsers;
    }

    [Required]
    public IEnumerable<OverviewUser> AllocatedUsers { get; }
        
    [Required]
    public IEnumerable<OverviewUser> InterruptedUsers { get; }
}