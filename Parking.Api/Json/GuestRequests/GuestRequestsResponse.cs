namespace Parking.Api.Json.GuestRequests;

using System.Collections.Generic;

public class GuestRequestsResponse(IEnumerable<GuestRequestsData> guestRequests)
{
    public IEnumerable<GuestRequestsData> GuestRequests { get; } = guestRequests;
}
