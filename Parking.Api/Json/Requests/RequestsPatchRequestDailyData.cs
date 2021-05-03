namespace Parking.Api.Json.Requests
{
    using System.ComponentModel.DataAnnotations;
    using NodaTime;

    public class RequestsPatchRequestDailyData
    {
        public RequestsPatchRequestDailyData(LocalDate localDate, bool requested)
        {
            this.LocalDate = localDate;
            this.Requested = requested;
        }
        
        [Required]
        public LocalDate LocalDate { get; }
        
        [Required]
        public bool Requested { get; }
    }
}