namespace Parking.Api.Json.Reservations
{
    using System.Collections.Generic;
    using NodaTime;

    public class ReservationsPatchRequestDailyData
    {
        public ReservationsPatchRequestDailyData(LocalDate localDate, IEnumerable<string> userIds)
        {
            this.LocalDate = localDate;
            this.UserIds = userIds;
        }
        
        public LocalDate LocalDate { get; }

        public IEnumerable<string> UserIds { get; }
    }
}