namespace Parking.Business.UnitTests;

using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Model;
using Moq;
using NodaTime;
using NodaTime.Testing.Extensions;
using Xunit;

public static class AllocationCreatorGuestTests
{
    private static readonly LocalDate AllocationDate = 28.November(2020);
    private static readonly List<User> Users = [];
    private static readonly List<Reservation> Reservations = [];
    private static readonly Configuration Configuration =
        new(nearbyDistance: 1, shortLeadTimeSpaces: 0, totalSpaces: 2);

    [Fact]
    public static void Allocates_guest_requests_before_regular_requests()
    {
        var guestRequests = new[]
        {
            new GuestRequest("g1", AllocationDate, "Guest1", "user1", null, GuestRequestStatus.Pending)
        };

        var regularRequests = new[]
        {
            new Request("user2", AllocationDate, RequestStatus.Interrupted)
        };

        var sortedRequests = new[] { regularRequests[0] };

        var result = CreateAllocationCreator(regularRequests, sortedRequests)
            .Create(AllocationDate, regularRequests, Reservations, Users, Configuration, LeadTimeType.Short, guestRequests);

        Assert.Single(result.AllocatedRequests);
        Assert.Equal("user2", result.AllocatedRequests.Single().UserId);

        Assert.Single(result.UpdatedGuestRequests);
        Assert.Equal(GuestRequestStatus.Allocated, result.UpdatedGuestRequests.Single().Status);
    }

    [Fact]
    public static void Interrupts_guests_when_no_spaces_available()
    {
        var config = new Configuration(nearbyDistance: 1, shortLeadTimeSpaces: 0, totalSpaces: 1);

        var guestRequests = new[]
        {
            new GuestRequest("g1", AllocationDate, "Guest1", "user1", null, GuestRequestStatus.Pending),
            new GuestRequest("g2", AllocationDate, "Guest2", "user1", null, GuestRequestStatus.Pending)
        };

        List<Request> regularRequests = [];

        var result = CreateAllocationCreator(regularRequests, [])
            .Create(AllocationDate, regularRequests, Reservations, Users, config, LeadTimeType.Short, guestRequests);

        Assert.Empty(result.AllocatedRequests);
        Assert.Equal(2, result.UpdatedGuestRequests.Count);

        var updatedGuests = result.UpdatedGuestRequests.OrderBy(g => g.Id).ToArray();
        Assert.Equal(GuestRequestStatus.Allocated, updatedGuests[0].Status);
        Assert.Equal(GuestRequestStatus.Interrupted, updatedGuests[1].Status);
    }

    [Fact]
    public static void Returns_empty_guest_list_when_no_guests()
    {
        List<Request> regularRequests = [];
        List<GuestRequest> guestRequests = [];

        var result = CreateAllocationCreator(regularRequests, [])
            .Create(AllocationDate, regularRequests, Reservations, Users, Configuration, LeadTimeType.Short, guestRequests);

        Assert.Empty(result.UpdatedGuestRequests);
    }

    [Fact]
    public static void Already_allocated_guests_count_toward_used_spaces()
    {
        var config = new Configuration(nearbyDistance: 1, shortLeadTimeSpaces: 0, totalSpaces: 2);

        var guestRequests = new[]
        {
            new GuestRequest("g1", AllocationDate, "Guest1", "user1", null, GuestRequestStatus.Allocated),
            new GuestRequest("g2", AllocationDate, "Guest2", "user1", null, GuestRequestStatus.Pending)
        };

        var regularRequests = new[]
        {
            new Request("user2", AllocationDate, RequestStatus.Interrupted)
        };

        var sortedRequests = new[] { regularRequests[0] };

        var result = CreateAllocationCreator(regularRequests, sortedRequests)
            .Create(AllocationDate, regularRequests, Reservations, Users, config, LeadTimeType.Short, guestRequests);

        // 2 total spaces, 1 already allocated guest = 1 free space
        // g2 (pending) takes that space, no room for regular requests
        Assert.Empty(result.AllocatedRequests);
        Assert.Single(result.UpdatedGuestRequests);
        Assert.Equal(GuestRequestStatus.Allocated, result.UpdatedGuestRequests.Single().Status);
        Assert.Equal("g2", result.UpdatedGuestRequests.Single().Id);
    }

    private static AllocationCreator CreateAllocationCreator(
        IReadOnlyCollection<Request> existingRequests,
        IReadOnlyCollection<Request> sortedRequests)
    {
        var mockRequestSorter = new Mock<IRequestSorter>(MockBehavior.Strict);
        mockRequestSorter
            .Setup(r => r.Sort(
                AllocationDate,
                It.IsAny<IReadOnlyCollection<Request>>(),
                Reservations,
                Users,
                Configuration.NearbyDistance))
            .Returns(sortedRequests);

        return new AllocationCreator(Mock.Of<ILogger<AllocationCreator>>(), mockRequestSorter.Object);
    }
}
