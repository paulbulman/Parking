namespace ParkingService.Model
{
    using NodaTime;

    public class Reservation
    {
        public Reservation(string userId, LocalDate date)
        {
            UserId = userId;
            Date = date;
        }

        public string UserId { get; }

        public LocalDate Date { get; }
    }
}