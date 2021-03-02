namespace Parking.Api.Json.Requests
{
    using NodaTime;

    public class RequestsPatchRequestDailyData
    {
        public RequestsPatchRequestDailyData(LocalDate localDate, bool requested)
        {
            this.LocalDate = localDate;
            this.Requested = requested;
        }
        
        public LocalDate LocalDate { get; }
        
        public bool Requested { get; }
    }
}