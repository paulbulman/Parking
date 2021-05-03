namespace Parking.Api.Json.Reservations
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
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

        [Required]
        public Calendar<ReservationsData> Reservations { get; }
        
        [Required]
        public int ShortLeadTimeSpaces { get; }

        [Required]
        public IEnumerable<ReservationsUser> Users { get; }
    }
}