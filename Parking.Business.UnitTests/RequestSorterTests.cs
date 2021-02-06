namespace Parking.Business.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Model;
    using NodaTime;
    using NodaTime.Testing.Extensions;
    using TestHelpers;
    using Xunit;

    public static class RequestSorterTests
    {
        private const int RandomSeed = 1;

        private const decimal NearbyDistance = 4;

        private static readonly LocalDate SortDate = 17.September(2020);

        private static readonly IReadOnlyCollection<Reservation> NoReservations = new List<Reservation>();

        private static readonly IReadOnlyCollection<User> DefaultUsers = new List<User>
        {
            CreateUser.With(userId: "user1", commuteDistance: NearbyDistance),
            CreateUser.With(userId: "user2", commuteDistance: NearbyDistance),
            CreateUser.With(userId: "user3", commuteDistance: NearbyDistance)
        };

        [Fact]
        public static void Returns_requests_for_given_date()
        {
            var requests = new[]
            {
                new Request("user1", SortDate, RequestStatus.Requested),
                new Request("user2", SortDate, RequestStatus.Requested),
                new Request("user3", SortDate, RequestStatus.Requested)
            };

            var result = SortRequests(requests, NoReservations, DefaultUsers);

            Assert.All(requests, r => Assert.Contains(r, result));
        }

        [Fact]
        public static void Does_not_return_requests_for_other_dates()
        {
            var requests = new[]
            {
                new Request("user1", SortDate.PlusDays(-1), RequestStatus.Requested),
                new Request("user1", SortDate.PlusDays(1), RequestStatus.Requested)
            };

            var result = SortRequests(requests, NoReservations, DefaultUsers);

            Assert.Empty(result);
        }

        [Fact]
        public static void Does_not_return_requests_for_other_statuses()
        {
            var requests = new[]
            {
                new Request("user1", SortDate, RequestStatus.Allocated),
                new Request("user2", SortDate, RequestStatus.Cancelled)
            };

            var result = SortRequests(requests, NoReservations, DefaultUsers);

            Assert.Empty(result);
        }

        [Fact]
        public static void Gives_priority_to_more_interrupted_users_over_less_interrupted_users()
        {
            var requests = new[]
            {
                new Request("user1", SortDate.PlusDays(-1), RequestStatus.Allocated),
                new Request("user2", SortDate.PlusDays(-1), RequestStatus.Requested),
                new Request("user1", SortDate, RequestStatus.Requested),
                new Request("user2", SortDate, RequestStatus.Requested)
            };

            var result = SortRequests(requests, NoReservations, DefaultUsers);

            CheckOrder(new[] { "user2", "user1" }, result);
        }

        [Fact]
        public static void Ignores_future_requests_when_calculating_priority()
        {
            var requests = new[]
            {
                new Request("user1", SortDate.PlusDays(-1), RequestStatus.Allocated),
                new Request("user2", SortDate.PlusDays(-1), RequestStatus.Requested),
                new Request("user1", SortDate, RequestStatus.Requested),
                new Request("user2", SortDate, RequestStatus.Requested),
                new Request("user1", SortDate.PlusDays(1), RequestStatus.Requested),
                new Request("user2", SortDate.PlusDays(1), RequestStatus.Allocated),
                new Request("user1", SortDate.PlusDays(2), RequestStatus.Requested),
                new Request("user2", SortDate.PlusDays(2), RequestStatus.Allocated)
            };

            var result = SortRequests(requests, NoReservations, DefaultUsers);

            CheckOrder(new[] { "user2", "user1" }, result);
        }

        [Fact]
        public static void Ignores_cancelled_requests_when_calculating_priority()
        {
            var requests = new[]
            {
                new Request("user2", SortDate.PlusDays(-3), RequestStatus.Cancelled),
                new Request("user2", SortDate.PlusDays(-2), RequestStatus.Cancelled),
                new Request("user1", SortDate.PlusDays(-1), RequestStatus.Allocated),
                new Request("user2", SortDate.PlusDays(-1), RequestStatus.Requested),
                new Request("user1", SortDate, RequestStatus.Requested),
                new Request("user2", SortDate, RequestStatus.Requested)
            };

            var result = SortRequests(requests, NoReservations, DefaultUsers);

            CheckOrder(new[] { "user2", "user1" }, result);
        }

        [Fact]
        public static void Uses_random_order_when_no_previous_requests_exist()
        {
            var requests = new[]
            {
                new Request("user1", SortDate, RequestStatus.Requested),
                new Request("user2", SortDate, RequestStatus.Requested),
                new Request("user3", SortDate, RequestStatus.Requested)
            };

            var result = SortRequests(requests, NoReservations, DefaultUsers);

            CheckOrder(new[] { "user2", "user1", "user3" }, result);
        }

        [Fact]
        public static void Orders_users_with_no_previous_requests_between_users_with_previous_requests()
        {
            var requests = new[]
            {
                new Request("user1", SortDate.PlusDays(-1), RequestStatus.Allocated),
                new Request("user2", SortDate.PlusDays(-1), RequestStatus.Requested),
                new Request("user1", SortDate, RequestStatus.Requested),
                new Request("user2", SortDate, RequestStatus.Requested),
                new Request("user3", SortDate, RequestStatus.Requested)
            };

            var result = SortRequests(requests, NoReservations, DefaultUsers);

            CheckOrder(new[] { "user2", "user3", "user1" }, result);
        }

        [Fact]
        public static void Preserves_order_when_interruption_ratios_are_equal()
        {
            var requests = new[]
            {
                new Request("user1", SortDate.PlusDays(-1), RequestStatus.Allocated),
                new Request("user2", SortDate.PlusDays(-1), RequestStatus.Allocated),
                new Request("user1", SortDate, RequestStatus.Requested),
                new Request("user2", SortDate, RequestStatus.Requested),
                new Request("user3", SortDate, RequestStatus.Requested)
            };

            var result = SortRequests(requests, NoReservations, DefaultUsers);

            CheckOrder(new[] { "user1", "user2", "user3" }, result);
        }

        [Fact]
        public static void Ignores_requests_with_reservations_when_calculating_priority()
        {
            var requests = new[]
            {
                new Request("user1", SortDate.PlusDays(-3), RequestStatus.Allocated),
                new Request("user2", SortDate.PlusDays(-3), RequestStatus.Requested),
                new Request("user1", SortDate.PlusDays(-2), RequestStatus.Requested),
                new Request("user2", SortDate.PlusDays(-2), RequestStatus.Allocated),
                new Request("user1", SortDate.PlusDays(-1), RequestStatus.Requested),
                new Request("user2", SortDate.PlusDays(-1), RequestStatus.Allocated),
                new Request("user1", SortDate, RequestStatus.Requested),
                new Request("user2", SortDate, RequestStatus.Requested),
            };

            var reservations = new[]
            {
                new Reservation("user2", SortDate.PlusDays(-2)),
                new Reservation("user2", SortDate.PlusDays(-1))
            };

            var result = SortRequests(requests, reservations, DefaultUsers);

            CheckOrder(new[] { "user2", "user1" }, result);
        }

        [Fact]
        public static void Gives_priority_to_far_away_users_over_more_interrupted_users()
        {
            var users = new[]
            {
                CreateUser.With(userId: "user1", commuteDistance: NearbyDistance),
                CreateUser.With(userId: "user2", commuteDistance: NearbyDistance + 0.01m)
            };

            var requests = new[]
            {
                new Request("user1", SortDate.PlusDays(-1), RequestStatus.Requested),
                new Request("user2", SortDate.PlusDays(-1), RequestStatus.Allocated),
                new Request("user1", SortDate, RequestStatus.Requested),
                new Request("user2", SortDate, RequestStatus.Requested)
            };

            var result = SortRequests(requests, NoReservations, users);

            CheckOrder(new[] { "user2", "user1" }, result);
        }

        [Fact]
        public static void Treats_users_with_missing_commute_distance_as_living_far_away()
        {
            var users = new[]
            {
                CreateUser.With(userId: "user1", commuteDistance: NearbyDistance),
                CreateUser.With(userId: "user2", commuteDistance: null)
            };

            var requests = new[]
            {
                new Request("user1", SortDate.PlusDays(-1), RequestStatus.Requested),
                new Request("user2", SortDate.PlusDays(-1), RequestStatus.Allocated),
                new Request("user1", SortDate, RequestStatus.Requested),
                new Request("user2", SortDate, RequestStatus.Requested)
            };

            var result = SortRequests(requests, NoReservations, users);

            CheckOrder(new[] { "user2", "user1" }, result);
        }

        [Fact]
        public static void Gives_priority_to_users_with_reservations_over_far_away_users()
        {
            var users = new[]
            {
                CreateUser.With(userId: "user1", commuteDistance: NearbyDistance + 1),
                CreateUser.With(userId: "user2", commuteDistance: NearbyDistance)
            };

            var requests = new[]
            {
                new Request("user1", SortDate.PlusDays(-1), RequestStatus.Requested),
                new Request("user2", SortDate.PlusDays(-1), RequestStatus.Allocated),
                new Request("user1", SortDate, RequestStatus.Requested),
                new Request("user2", SortDate, RequestStatus.Requested)
            };

            var reservations = new[]
            {
                new Reservation("user2", SortDate)
            };

            var result = SortRequests(requests, reservations, users);

            CheckOrder(new[] { "user2", "user1" }, result);
        }

        private static IEnumerable<Request> SortRequests(
            IReadOnlyCollection<Request> requests,
            IReadOnlyCollection<Reservation> reservations,
            IReadOnlyCollection<User> users) =>
            new RequestSorter(new Random(RandomSeed)).Sort(SortDate, requests, reservations, users, NearbyDistance);

        private static void CheckOrder(IEnumerable<string> expected, IEnumerable<Request> actual) =>
            Assert.Equal(expected, actual.Select(r => r.UserId));
    }
}