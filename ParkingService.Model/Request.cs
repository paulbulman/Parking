namespace ParkingService.Model
{
    using NodaTime;

    public class Request
    {
        public Request(string userId, LocalDate date, RequestStatus status)
        {
            UserId = userId;
            Date = date;
            Status = status;
        }

        public string UserId { get; }
        
        public LocalDate Date { get; }
        
        public RequestStatus Status { get; }
    }
}
