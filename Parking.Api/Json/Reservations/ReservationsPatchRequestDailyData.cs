namespace Parking.Api.Json.Reservations
{
    using NodaTime;

    public class ReservationsPatchRequestDailyData
    {
        public ReservationsPatchRequestDailyData(LocalDate date, ReservationsData reservations)
        {
            this.Date = date;
            this.Reservations = reservations;
        }
        
        public LocalDate Date { get; }
        
        public ReservationsData Reservations { get; }
    }
}