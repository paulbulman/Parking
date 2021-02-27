namespace Parking.Api.Json.Requests
{
    using NodaTime;

    public class RequestPatchRequestDailyData
    {
        public RequestPatchRequestDailyData(LocalDate date, bool requested)
        {
            this.Date = date;
            this.Requested = requested;
        }
        
        public LocalDate Date { get; }
        
        public bool Requested { get; }
    }
}