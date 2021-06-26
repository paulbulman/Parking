namespace Parking.Api.Json.Summary
{
    using System.ComponentModel.DataAnnotations;

    public class SummaryData
    {

        public SummaryData(SummaryStatus? status, bool isProblem)
        {
            this.Status = status;
            this.IsProblem = isProblem;
        }

        public SummaryStatus? Status { get; }
        
        [Required]
        public bool IsProblem { get; }
    }
}