namespace Parking.Business.UnitTests
{
    using System.Collections.Generic;
    using System.Linq;
    using Model;
    using Moq;
    using NodaTime;
    using NodaTime.Testing.Extensions;
    using Xunit;

    public static class AllocationCreatorTests
    {
        private static readonly LocalDate AllocationDate = 28.November(2020);

        // ReSharper disable once CollectionNeverUpdated.Local
        private static readonly List<User> Users = new List<User>();

        // ReSharper disable once CollectionNeverUpdated.Local
        private static readonly List<Reservation> Reservations = new List<Reservation>();

        private static readonly Configuration Configuration =
            new Configuration(nearbyDistance: 1, shortLeadTimeSpaces: 1, totalSpaces: 3);

        private static readonly IReadOnlyCollection<Request> RequestSorterResult = new[]
        {
            new Request("User3", AllocationDate, RequestStatus.Interrupted),
            new Request("User1", AllocationDate, RequestStatus.Interrupted),
            new Request("User2", AllocationDate, RequestStatus.Interrupted)
        };

        [Fact]
        public static void Creates_allocated_requests_from_the_sorted_requests()
        {
            // ReSharper disable once CollectionNeverUpdated.Local
            var existingRequests = new List<Request>();

            var actual = CreateAllocationCreator(existingRequests)
                .Create(AllocationDate, existingRequests, Reservations, Users, Configuration, LeadTimeType.Short)
                .ToList();

            var expected = RequestSorterResult.ToList();

            CheckRequests(expected, actual);
        }

        [Fact]
        public static void Does_not_use_short_lead_time_spaces_at_long_lead_time()
        {
            // ReSharper disable once CollectionNeverUpdated.Local
            var existingRequests = new List<Request>();

            var actual = CreateAllocationCreator(existingRequests)
                .Create(AllocationDate, existingRequests, Reservations, Users, Configuration, LeadTimeType.Long)
                .ToList();

            var expected = RequestSorterResult.Take(2).ToList();

            CheckRequests(expected, actual);
        }

        [Fact]
        public static void Only_considers_requests_for_same_date_when_calculating_already_allocated_spaces()
        {
            var existingRequests = new[]
            {
                new Request("AlreadyAllocated1", AllocationDate, RequestStatus.Allocated),
                new Request("AlreadyAllocated2", AllocationDate, RequestStatus.Allocated),
                new Request("OtherDate1", AllocationDate.PlusDays(1), RequestStatus.Allocated),
                new Request("OtherDate2", AllocationDate.PlusDays(-1), RequestStatus.Allocated)
            };

            var actual = CreateAllocationCreator(existingRequests)
                .Create(AllocationDate, existingRequests, Reservations, Users, Configuration, LeadTimeType.Short)
                .ToList();

            var expected = RequestSorterResult.Take(1).ToList();

            CheckRequests(expected, actual);
        }

        [Fact]
        public static void Only_considers_allocated_requests_when_calculating_already_allocated_spaces()
        {
            var existingRequests = new[]
            {
                new Request("AlreadyAllocated1", AllocationDate, RequestStatus.Allocated),
                new Request("AlreadyAllocated2", AllocationDate, RequestStatus.Allocated),
                new Request("OtherStatus1", AllocationDate, RequestStatus.Cancelled),
                new Request("OtherStatus2", AllocationDate, RequestStatus.Interrupted),
                new Request("OtherStatus3", AllocationDate, RequestStatus.SoftInterrupted),
                new Request("OtherStatus4", AllocationDate, RequestStatus.HardInterrupted),
            };

            var actual = CreateAllocationCreator(existingRequests)
                .Create(AllocationDate, existingRequests, Reservations, Users, Configuration, LeadTimeType.Short)
                .ToList();

            var expected = RequestSorterResult.Take(1).ToList();

            CheckRequests(expected, actual);
        }

        private static AllocationCreator CreateAllocationCreator(IReadOnlyCollection<Request> existingRequests)
        {
            var mockRequestSorter = new Mock<IRequestSorter>(MockBehavior.Strict);
            mockRequestSorter
                .Setup(r => r.Sort(AllocationDate, existingRequests, Reservations, Users, Configuration.NearbyDistance))
                .Returns(RequestSorterResult);

            return new AllocationCreator(mockRequestSorter.Object);
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private static void CheckRequests(IReadOnlyList<Request> expected, IReadOnlyList<Request> actual)
        {
            Assert.NotNull(actual);
            Assert.Equal(expected.Count, actual.Count);

            for (var i = 0; i < expected.Count; i++)
            {
                Assert.Equal(expected[i].UserId, actual[i].UserId);
                Assert.Equal(expected[i].Date, actual[i].Date);
                Assert.Equal(RequestStatus.Allocated, actual[i].Status);
            }
        }
    }
}
