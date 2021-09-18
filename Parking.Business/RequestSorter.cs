namespace Parking.Business
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Extensions.Logging;
    using Model;
    using NodaTime;

    public interface IRequestSorter
    {
        IReadOnlyCollection<Request> Sort(
            LocalDate date,
            IReadOnlyCollection<Request> requests,
            IReadOnlyCollection<Reservation> reservations,
            IReadOnlyCollection<User> users,
            decimal nearbyDistance);
    }

    public class RequestSorter : IRequestSorter
    {
        private readonly ILogger<RequestSorter> logger;
        private readonly Random random;

        public RequestSorter(
            ILogger<RequestSorter> logger,
            Random random)
        {
            this.logger = logger;
            this.random = random;
        }

        public IReadOnlyCollection<Request> Sort(
            LocalDate date,
            IReadOnlyCollection<Request> requests,
            IReadOnlyCollection<Reservation> reservations,
            IReadOnlyCollection<User> users,
            decimal nearbyDistance)
        {
            var requestsToSort = requests
                .Where(r => r.Date == date && r.Status.IsAllocatable())
                .ToArray();

            if (!requestsToSort.Any())
            {
                return new List<Request>();
            }

            var fullNames = users.ToDictionary(u => u.UserId, u => $"{u.FirstName} {u.LastName}");

            var existingAllocationRatios = this.CalculateExistingAllocationRatios(
                fullNames,
                requestsToSort,
                requests,
                reservations);

            var sortableRequests = requestsToSort
                .Select(r => new SortableRequest(
                    r.UserId,
                    r.Date,
                    r.Status,
                    fullName: fullNames[r.UserId],
                    hasReservation: UserHasReservation(r, reservations),
                    livesFarAway: UserLivesFarAway(r, users, nearbyDistance),
                    existingAllocationRatio: existingAllocationRatios[r]));

            var sortedRequests = sortableRequests
                .OrderBy(r => r.HasReservation ? 0 : 1)
                .ThenBy(r => r.LivesFarAway ? 0 : 1)
                .ThenBy(r => r.ExistingAllocationRatio)
                .ToArray();

            this.logger.LogDebug("Sorted requests for {@date}: {@sortedRequests}", date, sortedRequests);

            return sortedRequests
                .Select(r => new Request(r.UserId, r.LocalDate, r.InitialRequestStatus))
                .ToArray();
        }

        private IDictionary<Request, decimal> CalculateExistingAllocationRatios(
            IDictionary<string, string> fullNames,
            IReadOnlyCollection<Request> requestsToSort,
            IReadOnlyCollection<Request> allRequests,
            IReadOnlyCollection<Reservation> reservations)
        {
            var existingAllocationRatios = requestsToSort.ToDictionary(
                r => r,
                r => CalculateExistingAllocationRatio(r, allRequests, reservations));

            var fullNamesWithoutAllocationRatios = existingAllocationRatios
                .Where(r => r.Value == null)
                .Select(r => fullNames[r.Key.UserId])
                .ToArray();

            if (fullNamesWithoutAllocationRatios.Any())
            {
                this.logger.LogDebug(
                    "{@fullNames} had no existing allocation ratios and will have a value created at random for {@date}.",
                    fullNamesWithoutAllocationRatios,
                    requestsToSort.Select(r => r.Date).Distinct().Single());
            }

            var minExistingRatio = existingAllocationRatios.Select(r => r.Value).Min() ?? 0;
            var maxExistingRatio = existingAllocationRatios.Select(r => r.Value).Max() ?? 1;

            var minExistingPercentage = (int)(minExistingRatio * 100);
            var maxExistingPercentage = (int)(maxExistingRatio * 100);

            // Random.Next is inclusive on minValue and exclusive on maxValue,
            // whereas we want a value strictly between the two, if different.
            var minValue = Math.Min(minExistingPercentage + 1, maxExistingPercentage);
            var maxValue = maxExistingPercentage;

            return existingAllocationRatios.ToDictionary(
                r => r.Key,
                r => r.Value ?? (decimal)random.Next(minValue, maxValue) / 100);
        }

        private static decimal? CalculateExistingAllocationRatio(
            Request request,
            IReadOnlyCollection<Request> allRequests,
            IReadOnlyCollection<Reservation> reservations)
        {
            var userPreviousRequests = allRequests
                .Where(r =>
                    r.UserId == request.UserId &&
                    r.Date < request.Date &&
                    r.Date >= new LocalDate(2021, 9, 6) &&
                    !UserHasReservation(r, reservations))
                .ToArray();

            var userPreviousAllocatedRequestCount = userPreviousRequests.Count(r =>
                r.Status == RequestStatus.Allocated);

            var userPreviousTotalRequestCount = userPreviousRequests.Count(r => r.Status.IsRequested());

            return userPreviousTotalRequestCount == 0 ? (decimal?)null : (decimal)userPreviousAllocatedRequestCount / userPreviousTotalRequestCount;
        }

        private static bool UserHasReservation(Request request, IEnumerable<Reservation> reservations) =>
            reservations.Any(r => r.UserId == request.UserId && r.Date == request.Date);

        private static bool UserLivesFarAway(Request request, IEnumerable<User> users, decimal nearbyDistance)
        {
            var commuteDistance = users
                .Single(u => u.UserId == request.UserId)
                .CommuteDistance;

            return commuteDistance == null || commuteDistance > nearbyDistance;
        }

        private class SortableRequest
        {
            public SortableRequest(
                string userId,
                LocalDate localDate,
                RequestStatus initialRequestStatus,
                string fullName,
                bool hasReservation,
                bool livesFarAway,
                decimal? existingAllocationRatio)
            {
                this.UserId = userId;
                this.LocalDate = localDate;
                this.InitialRequestStatus = initialRequestStatus;
                this.FullName = fullName;
                this.HasReservation = hasReservation;
                this.LivesFarAway = livesFarAway;
                this.ExistingAllocationRatio = existingAllocationRatio;
            }

            public string UserId { get; }

            public LocalDate LocalDate { get; }

            public RequestStatus InitialRequestStatus { get; }

            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            // ReSharper disable once MemberCanBePrivate.Local
            // This property is useful for logging.
            public string FullName { get; }

            public bool HasReservation { get; }

            public bool LivesFarAway { get; }

            public decimal? ExistingAllocationRatio { get; }
        }
    }
}