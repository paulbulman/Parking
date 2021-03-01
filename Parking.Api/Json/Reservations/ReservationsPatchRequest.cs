namespace Parking.Api.Json.Reservations
{
    using System.Collections.Generic;

    public class ReservationsPatchRequest
    {
        public ReservationsPatchRequest(IEnumerable<ReservationsPatchRequestDailyData> reservations) =>
            this.Reservations = reservations;

        public IEnumerable<ReservationsPatchRequestDailyData> Reservations { get; }
    }
}