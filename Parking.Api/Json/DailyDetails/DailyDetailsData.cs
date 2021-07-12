namespace Parking.Api.Json.DailyDetails
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class DailyDetailsData
    {
        public DailyDetailsData(
            IEnumerable<DailyDetailsUser> allocatedUsers,
            IEnumerable<DailyDetailsUser> interruptedUsers,
            IEnumerable<DailyDetailsUser> requestedUsers)
        {
            this.AllocatedUsers = allocatedUsers;
            this.InterruptedUsers = interruptedUsers;
            this.RequestedUsers = requestedUsers;
        }

        [Required]
        public IEnumerable<DailyDetailsUser> AllocatedUsers { get; }
        
        [Required]
        public IEnumerable<DailyDetailsUser> InterruptedUsers { get; }
        
        [Required]
        public IEnumerable<DailyDetailsUser> RequestedUsers { get; }
    }
}