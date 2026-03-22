namespace Parking.Data.UnitTests;

using System.Collections.Generic;
using System.Threading.Tasks;
using Aws;
using Microsoft.Extensions.Logging;
using Model;
using Moq;
using NodaTime;
using NodaTime.Testing.Extensions;
using Parking.Business.Data;
using Xunit;

public static class GuestRequestRepositoryTests
{
    [Fact]
    public static async Task GetGuestRequests_returns_empty_collection_when_no_matching_raw_item_exists()
    {
        var mockDatabaseProvider = new Mock<IDatabaseProvider>(MockBehavior.Strict);

        mockDatabaseProvider
            .Setup(p => p.GetGuests(new YearMonth(2026, 3)))
            .ReturnsAsync(System.Array.Empty<RawItem>());

        var repository = new GuestRequestRepository(
            Mock.Of<ILogger<GuestRequestRepository>>(),
            mockDatabaseProvider.Object);

        var result = await repository.GetGuestRequests(new DateInterval(1.March(2026), 31.March(2026)));

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public static async Task GetGuestRequests_converts_raw_items_to_guest_requests()
    {
        var mockDatabaseProvider = new Mock<IDatabaseProvider>(MockBehavior.Strict);

        mockDatabaseProvider
            .Setup(p => p.GetGuests(new YearMonth(2026, 3)))
            .ReturnsAsync(new[]
            {
                CreateRawItem(
                    "2026-03",
                    KeyValuePair.Create("10", new List<GuestData>
                    {
                        new GuestData { Id = "g1", Name = "Alice Smith", VisitingUserId = "user1", RegistrationNumber = "AB12CDE", Status = "P" }
                    }),
                    KeyValuePair.Create("20", new List<GuestData>
                    {
                        new GuestData { Id = "g2", Name = "Bob Jones", VisitingUserId = "user2", RegistrationNumber = null, Status = "A" }
                    }))
            });

        mockDatabaseProvider
            .Setup(p => p.GetGuests(new YearMonth(2026, 4)))
            .ReturnsAsync(new[]
            {
                CreateRawItem(
                    "2026-04",
                    KeyValuePair.Create("05", new List<GuestData>
                    {
                        new GuestData { Id = "g3", Name = "Carol White", VisitingUserId = "user1", RegistrationNumber = "XY99ZZZ", Status = "I" }
                    }))
            });

        var repository = new GuestRequestRepository(
            Mock.Of<ILogger<GuestRequestRepository>>(),
            mockDatabaseProvider.Object);

        var result = await repository.GetGuestRequests(new DateInterval(1.March(2026), 30.April(2026)));

        Assert.NotNull(result);
        Assert.Equal(3, result.Count);

        CheckGuestRequest(result, "g1", 10.March(2026), "Alice Smith", "user1", "AB12CDE", GuestRequestStatus.Pending);
        CheckGuestRequest(result, "g2", 20.March(2026), "Bob Jones", "user2", null, GuestRequestStatus.Allocated);
        CheckGuestRequest(result, "g3", 5.April(2026), "Carol White", "user1", "XY99ZZZ", GuestRequestStatus.Interrupted);
    }

    [Fact]
    public static async Task GetGuestRequests_filters_guests_outside_specified_date_range()
    {
        var mockDatabaseProvider = new Mock<IDatabaseProvider>(MockBehavior.Strict);

        mockDatabaseProvider
            .Setup(p => p.GetGuests(new YearMonth(2026, 3)))
            .ReturnsAsync(new[]
            {
                CreateRawItem(
                    "2026-03",
                    KeyValuePair.Create("10", new List<GuestData>
                    {
                        new GuestData { Id = "g1", Name = "Alice Smith", VisitingUserId = "user1", RegistrationNumber = "AB12CDE", Status = "P" }
                    }),
                    KeyValuePair.Create("20", new List<GuestData>
                    {
                        new GuestData { Id = "g2", Name = "Bob Jones", VisitingUserId = "user2", RegistrationNumber = null, Status = "A" }
                    }))
            });

        var repository = new GuestRequestRepository(
            Mock.Of<ILogger<GuestRequestRepository>>(),
            mockDatabaseProvider.Object);

        var result = await repository.GetGuestRequests(new DateInterval(15.March(2026), 31.March(2026)));

        Assert.NotNull(result);
        Assert.Single(result);

        CheckGuestRequest(result, "g2", 20.March(2026), "Bob Jones", "user2", null, GuestRequestStatus.Allocated);
    }

    [Fact]
    public static async Task SaveGuestRequest_saves_single_guest()
    {
        var mockDatabaseProvider = new Mock<IDatabaseProvider>(MockBehavior.Strict);

        mockDatabaseProvider
            .Setup(p => p.GetGuests(new YearMonth(2026, 3)))
            .ReturnsAsync(System.Array.Empty<RawItem>());

        RawItem? savedItem = null;
        mockDatabaseProvider
            .Setup(p => p.SaveItem(It.IsAny<RawItem>()))
            .Callback<RawItem>(item => savedItem = item)
            .Returns(Task.CompletedTask);

        var repository = new GuestRequestRepository(
            Mock.Of<ILogger<GuestRequestRepository>>(),
            mockDatabaseProvider.Object);

        var guestRequest = new GuestRequest(
            id: "g1",
            date: 15.March(2026),
            name: "Alice Smith",
            visitingUserId: "user1",
            registrationNumber: "AB12CDE",
            status: GuestRequestStatus.Pending);

        await repository.SaveGuestRequest(guestRequest);

        Assert.NotNull(savedItem);
        Assert.Equal("GLOBAL", savedItem.PrimaryKey);
        Assert.Equal("GUESTS#2026-03", savedItem.SortKey);

        Assert.NotNull(savedItem.Guests);
        Assert.Single(savedItem.Guests);

        var dayGuests = savedItem.Guests["15"];
        Assert.Single(dayGuests);
        Assert.Equal("g1", dayGuests[0].Id);
        Assert.Equal("Alice Smith", dayGuests[0].Name);
        Assert.Equal("user1", dayGuests[0].VisitingUserId);
        Assert.Equal("AB12CDE", dayGuests[0].RegistrationNumber);
        Assert.Equal("P", dayGuests[0].Status);
    }

    [Fact]
    public static async Task SaveGuestRequests_handles_empty_list()
    {
        var mockDatabaseProvider = new Mock<IDatabaseProvider>();

        var repository = new GuestRequestRepository(
            Mock.Of<ILogger<GuestRequestRepository>>(),
            mockDatabaseProvider.Object);

        await repository.SaveGuestRequests([]);

        mockDatabaseProvider.VerifyNoOtherCalls();
    }

    [Fact]
    public static async Task DeleteGuestRequest_removes_guest_and_saves()
    {
        var mockDatabaseProvider = new Mock<IDatabaseProvider>(MockBehavior.Strict);

        mockDatabaseProvider
            .Setup(p => p.GetGuests(new YearMonth(2026, 3)))
            .ReturnsAsync(new[]
            {
                CreateRawItem(
                    "2026-03",
                    KeyValuePair.Create("15", new List<GuestData>
                    {
                        new GuestData { Id = "g1", Name = "Alice Smith", VisitingUserId = "user1", RegistrationNumber = "AB12CDE", Status = "P" },
                        new GuestData { Id = "g2", Name = "Bob Jones", VisitingUserId = "user2", RegistrationNumber = null, Status = "A" }
                    }))
            });

        RawItem? savedItem = null;
        mockDatabaseProvider
            .Setup(p => p.SaveItem(It.IsAny<RawItem>()))
            .Callback<RawItem>(item => savedItem = item)
            .Returns(Task.CompletedTask);

        var repository = new GuestRequestRepository(
            Mock.Of<ILogger<GuestRequestRepository>>(),
            mockDatabaseProvider.Object);

        await repository.DeleteGuestRequest(15.March(2026), "g1");

        Assert.NotNull(savedItem);
        Assert.NotNull(savedItem.Guests);
        Assert.Single(savedItem.Guests);

        var dayGuests = savedItem.Guests["15"];
        Assert.Single(dayGuests);
        Assert.Equal("g2", dayGuests[0].Id);
        Assert.Equal("Bob Jones", dayGuests[0].Name);
    }

    private static RawItem CreateRawItem(
        string monthKey,
        params KeyValuePair<string, List<GuestData>>[] guestData) =>
        RawItem.CreateGuests(
            primaryKey: "GLOBAL",
            sortKey: $"GUESTS#{monthKey}",
            guests: new Dictionary<string, List<GuestData>>(guestData));

    private static void CheckGuestRequest(
        System.Collections.Generic.IEnumerable<GuestRequest> result,
        string expectedId,
        LocalDate expectedDate,
        string expectedName,
        string expectedVisitingUserId,
        string? expectedRegistrationNumber,
        GuestRequestStatus expectedStatus)
    {
        var matches = System.Linq.Enumerable.Where(result, g =>
            g.Id == expectedId &&
            g.Date == expectedDate &&
            g.Name == expectedName &&
            g.VisitingUserId == expectedVisitingUserId &&
            g.RegistrationNumber == expectedRegistrationNumber &&
            g.Status == expectedStatus);

        Assert.Single(matches);
    }
}
