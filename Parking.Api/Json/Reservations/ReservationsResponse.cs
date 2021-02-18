namespace Parking.Api.Json.Reservations
{
    using System.Collections.Generic;
    using Calendar;

    public class ReservationsResponse
    {
        public ReservationsResponse(
            Calendar<ReservationsData> reservations,
            int shortLeadTimeSpaces,
            IEnumerable<ReservationsUser> users)
        {
            this.Reservations = reservations;
            this.ShortLeadTimeSpaces = shortLeadTimeSpaces;
            this.Users = users;
        }

        public Calendar<ReservationsData> Reservations { get; }
        
        public int ShortLeadTimeSpaces { get; }

        public IEnumerable<ReservationsUser> Users { get; }
    }
}