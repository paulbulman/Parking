namespace ParkingService.Business
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Model;
    using NodaTime;

    public class AllocationCreator
    {
        private readonly IRequestSorter requestSorter;

        public AllocationCreator(IRequestSorter requestSorter) => this.requestSorter = requestSorter;

        public IReadOnlyCollection<Request> Create(
            LocalDate date,
            IReadOnlyCollection<Request> requests,
            IReadOnlyCollection<Reservation> reservations,
            IReadOnlyCollection<User> users,
            Configuration configuration,
            bool shortLeadTime)
        {
            var spacesToReserve = shortLeadTime ? 0 : configuration.ShortLeadTimeSpaces;
            var allocatableSpaces = configuration.TotalSpaces - spacesToReserve;
            var alreadyAllocatedSpaces = requests.Count(r => r.Date == date && r.Status == RequestStatus.Allocated);
            var freeSpaces = allocatableSpaces - alreadyAllocatedSpaces;

            var sortedRequests = this.requestSorter
                .Sort(date, requests, reservations, users, configuration.NearbyDistance)
                .ToArray();

            return sortedRequests
                .Take(Math.Min(freeSpaces, sortedRequests.Length))
                .Select(r => new Request(r.UserId, r.Date, RequestStatus.Allocated))
                .ToArray();
        }
    }
}