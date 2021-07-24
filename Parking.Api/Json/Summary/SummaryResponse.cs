using Parking.Api.Json.Calendar;

namespace Parking.Api.Json.Summary
{
    using System.ComponentModel.DataAnnotations;

    public class SummaryResponse
    {
        public SummaryResponse(Calendar<SummaryData> summary, StayInterruptedStatus stayInterruptedStatus)
        {
            this.Summary = summary;
            this.StayInterruptedStatus = stayInterruptedStatus;
        }

        [Required]
        public Calendar<SummaryData> Summary { get; }

        [Required]
        public StayInterruptedStatus StayInterruptedStatus { get; }
    }
}