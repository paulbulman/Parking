namespace Parking.Business;

using System.Collections.Generic;
using Model;

public class AllocationResult(
    IReadOnlyCollection<Request> allocatedRequests,
    IReadOnlyCollection<GuestRequest> updatedGuestRequests)
{
    public IReadOnlyCollection<Request> AllocatedRequests { get; } = allocatedRequests;
    public IReadOnlyCollection<GuestRequest> UpdatedGuestRequests { get; } = updatedGuestRequests;
}
