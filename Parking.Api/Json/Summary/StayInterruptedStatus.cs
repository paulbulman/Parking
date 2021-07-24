namespace Parking.Api.Json.Summary
{
    using System.ComponentModel.DataAnnotations;
    using NodaTime;

    public class StayInterruptedStatus
    {
        public StayInterruptedStatus(LocalDate localDate, bool isAllowed, bool isSet)
        {
            this.LocalDate = localDate;
            this.IsAllowed = isAllowed;
            this.IsSet = isSet;
        }

        [Required]
        public LocalDate LocalDate { get; }
        
        [Required]
        public bool IsAllowed { get; }
        
        [Required]
        public bool IsSet { get; }
    }
}