namespace Parking.Api.Json.Overview
{
    using System.Collections.Generic;

    public class OverviewData
    {
        public OverviewData(IEnumerable<OverviewUser> allocatedUsers, IEnumerable<OverviewUser> interruptedUsers)
        {
            this.AllocatedUsers = allocatedUsers;
            this.InterruptedUsers = interruptedUsers;
        }

        public IEnumerable<OverviewUser> AllocatedUsers { get; }
        
        public IEnumerable<OverviewUser> InterruptedUsers { get; }
    }
}