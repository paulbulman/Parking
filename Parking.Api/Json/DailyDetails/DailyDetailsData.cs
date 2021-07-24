namespace Parking.Api.Json.DailyDetails
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class DailyDetailsData
    {
        public DailyDetailsData(
            IEnumerable<DailyDetailsUser> allocatedUsers,
            IEnumerable<DailyDetailsUser> interruptedUsers,
            IEnumerable<DailyDetailsUser> pendingUsers)
        {
            this.AllocatedUsers = allocatedUsers;
            this.InterruptedUsers = interruptedUsers;
            this.PendingUsers = pendingUsers;
        }

        [Required]
        public IEnumerable<DailyDetailsUser> AllocatedUsers { get; }
        
        [Required]
        public IEnumerable<DailyDetailsUser> InterruptedUsers { get; }
        
        [Required]
        public IEnumerable<DailyDetailsUser> PendingUsers { get; }
    }
}