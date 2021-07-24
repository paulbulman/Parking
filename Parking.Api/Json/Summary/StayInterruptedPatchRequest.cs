namespace Parking.Api.Json.Summary
{
    using System.ComponentModel.DataAnnotations;
    using NodaTime;

    public class StayInterruptedPatchRequest
    {
        public StayInterruptedPatchRequest(LocalDate localDate, bool stayInterrupted)
        {
            this.LocalDate = localDate;
            this.StayInterrupted = stayInterrupted;
        }

        [Required]
        public LocalDate LocalDate { get; }

        [Required]
        public bool StayInterrupted { get; }
    }
}