namespace Parking.Api.Json.History;

using System.ComponentModel.DataAnnotations;
using Calendar;

public class HistoryResponse(
    Calendar<string> history,
    int allocatedContestedRequestsCount,
    int totalContestedRequestsCount,
    decimal allocationRatio)
{
    [Required]
    public Calendar<string> History { get; } = history;

    [Required]
    public int TotalContestedRequestsCount { get; } = totalContestedRequestsCount;

    [Required]
    public int AllocatedContestedRequestsCount { get; } = allocatedContestedRequestsCount;

    [Required]
    public decimal AllocationRatio { get; } = allocationRatio;
}