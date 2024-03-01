namespace Parking.Api.Json.Summary;

using System.ComponentModel.DataAnnotations;

public class SummaryData(SummaryStatus? status, bool isProblem)
{
    public SummaryStatus? Status { get; } = status;

    [Required]
    public bool IsProblem { get; } = isProblem;
}