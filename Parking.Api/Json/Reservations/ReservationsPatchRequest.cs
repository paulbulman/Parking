namespace Parking.Api.Json.Reservations
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class ReservationsPatchRequest
    {
        public ReservationsPatchRequest(IEnumerable<ReservationsPatchRequestDailyData> reservations) =>
            this.Reservations = reservations;

        [Required]
        public IEnumerable<ReservationsPatchRequestDailyData> Reservations { get; }
    }
}