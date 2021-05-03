namespace Parking.Api.Json.Reservations
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class ReservationsData
    {
        public ReservationsData(IEnumerable<string> userIds) => this.UserIds = userIds;

        [Required]
        public IEnumerable<string> UserIds { get; }
    }
}