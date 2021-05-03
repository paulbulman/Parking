namespace Parking.Api.Json.Reservations
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using NodaTime;

    public class ReservationsPatchRequestDailyData
    {
        public ReservationsPatchRequestDailyData(LocalDate localDate, IEnumerable<string> userIds)
        {
            this.LocalDate = localDate;
            this.UserIds = userIds;
        }
        
        [Required]
        public LocalDate LocalDate { get; }

        [Required]
        public IEnumerable<string> UserIds { get; }
    }
}