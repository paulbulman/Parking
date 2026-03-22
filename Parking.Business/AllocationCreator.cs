namespace Parking.Business
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Extensions.Logging;
    using Model;
    using NodaTime;

    public interface IAllocationCreator
    {
        AllocationResult Create(
            LocalDate date,
            IReadOnlyCollection<Request> requests,
            IReadOnlyCollection<Reservation> reservations,
            IReadOnlyCollection<User> users,
            Configuration configuration,
            LeadTimeType leadTimeType,
            IReadOnlyCollection<GuestRequest> guestRequests);
    }

    public class AllocationCreator : IAllocationCreator
    {
        private readonly ILogger<AllocationCreator> logger;
        private readonly IRequestSorter requestSorter;

        public AllocationCreator(
            ILogger<AllocationCreator> logger,
            IRequestSorter requestSorter)
        {
            this.logger = logger;
            this.requestSorter = requestSorter;
        }

        public AllocationResult Create(
            LocalDate date,
            IReadOnlyCollection<Request> requests,
            IReadOnlyCollection<Reservation> reservations,
            IReadOnlyCollection<User> users,
            Configuration configuration,
            LeadTimeType leadTimeType,
            IReadOnlyCollection<GuestRequest> guestRequests)
        {
            var spacesToReserve = leadTimeType == LeadTimeType.Short ? 0 : configuration.ShortLeadTimeSpaces;
            var allocatableSpaces = configuration.TotalSpaces - spacesToReserve;

            var alreadyAllocatedRegular = requests.Count(r => r.Date == date && r.Status == RequestStatus.Allocated);
            var alreadyAllocatedGuests = guestRequests.Count(g => g.Date == date && g.Status == GuestRequestStatus.Allocated);
            var alreadyAllocatedSpaces = alreadyAllocatedRegular + alreadyAllocatedGuests;

            var freeSpaces = allocatableSpaces - alreadyAllocatedSpaces;

            if (freeSpaces < 0)
            {
                this.logger.LogWarning("{freeSpaces} free spaces for {@date}.", freeSpaces, date);
            }

            // Allocate pending/interrupted guest requests first
            var pendingGuests = guestRequests
                .Where(g => g.Date == date && g.Status is GuestRequestStatus.Pending or GuestRequestStatus.Interrupted)
                .ToArray();

            var guestsToAllocate = Math.Min(pendingGuests.Length, Math.Max(0, freeSpaces));

            var updatedGuestRequests = pendingGuests
                .Select((g, i) => new GuestRequest(
                    g.Id, g.Date, g.Name, g.VisitingUserId, g.RegistrationNumber,
                    i < guestsToAllocate ? GuestRequestStatus.Allocated : GuestRequestStatus.Interrupted))
                .ToArray();

            freeSpaces = Math.Max(0, freeSpaces - guestsToAllocate);

            if (freeSpaces <= 0)
            {
                return new AllocationResult(new List<Request>(), updatedGuestRequests);
            }

            var sortedRequests = this.requestSorter
                .Sort(date, requests, reservations, users, configuration.NearbyDistance)
                .ToArray();

            var allocatedRequests = sortedRequests
                .Take(Math.Min(freeSpaces, sortedRequests.Length))
                .Select(r => new Request(r.UserId, r.Date, RequestStatus.Allocated))
                .ToArray();

            if (allocatedRequests.Any())
            {
                this.logger.LogDebug(
                    "Allocating {allocatedRequestsCount} of {sortedRequestsCount} requests for {@date}.",
                    allocatedRequests.Length,
                    sortedRequests.Length,
                    date);
            }

            return new AllocationResult(allocatedRequests, updatedGuestRequests);
        }
    }
}
