namespace Parking.Api.Json.History;

using System.ComponentModel.DataAnnotations;
using Calendar;

public class HistoryResponse
{
    public HistoryResponse(
        Calendar<string> history,
        int allocatedContestedRequestsCount,
        int totalContestedRequestsCount,
        decimal allocationRatio)
    {
        this.History = history;
        this.AllocatedContestedRequestsCount = allocatedContestedRequestsCount;
        this.TotalContestedRequestsCount = totalContestedRequestsCount;
        this.AllocationRatio = allocationRatio;
    }

    [Required]
    public Calendar<string> History { get; }

    [Required]
    public int TotalContestedRequestsCount { get; }
        
    [Required]
    public int AllocatedContestedRequestsCount { get; }
        
    [Required]
    public decimal AllocationRatio { get; }
}