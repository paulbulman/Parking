namespace Parking.Business
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
        private readonly Random random;

        public RequestSorter(Random random) => this.random = random;

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

            var existingAllocationRatios = CalculateExistingAllocationRatios(requestsToSort, requests, reservations);

            return requestsToSort
                .OrderBy(r => UserHasReservation(r, reservations) ? 0 : 1)
                .ThenBy(r => UserLivesFarAway(r, users, nearbyDistance) ? 0 : 1)
                .ThenBy(r => existingAllocationRatios[r])
                .ToArray();
        }

        private IDictionary<Request, decimal> CalculateExistingAllocationRatios(
            IReadOnlyCollection<Request> requestsToSort,
            IReadOnlyCollection<Request> allRequests,
            IReadOnlyCollection<Reservation> reservations)
        {
            var existingAllocationRatios = requestsToSort.ToDictionary(
                r => r,
                r => CalculateExistingAllocationRatio(r, allRequests, reservations));

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
    }
}