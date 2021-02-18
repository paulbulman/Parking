namespace Parking.Api.Json.Reservations
{
    using System.Collections.Generic;

    public class ReservationsData
    {
        public ReservationsData(IEnumerable<string> userIds) => this.UserIds = userIds;

        public IEnumerable<string> UserIds { get; }
    }
}