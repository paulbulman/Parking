﻿namespace Parking.Business
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Extensions.Logging;
    using Model;
    using NodaTime;

    public interface IAllocationCreator
    {
        IReadOnlyCollection<Request> Create(
            LocalDate date,
            IReadOnlyCollection<Request> requests,
            IReadOnlyCollection<Reservation> reservations,
            IReadOnlyCollection<User> users,
            Configuration configuration,
            LeadTimeType leadTimeType);
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

        public IReadOnlyCollection<Request> Create(
            LocalDate date,
            IReadOnlyCollection<Request> requests,
            IReadOnlyCollection<Reservation> reservations,
            IReadOnlyCollection<User> users,
            Configuration configuration,
            LeadTimeType leadTimeType)
        {
            var spacesToReserve = leadTimeType == LeadTimeType.Short ? 0 : configuration.ShortLeadTimeSpaces;
            var allocatableSpaces = configuration.TotalSpaces - spacesToReserve;
            var alreadyAllocatedSpaces = requests.Count(r => r.Date == date && r.Status == RequestStatus.Allocated);
            var freeSpaces = allocatableSpaces - alreadyAllocatedSpaces;

            if (freeSpaces < 0)
            {
                this.logger.LogWarning("{freeSpaces} free spaces for {@date}.", freeSpaces, date);
                return new List<Request>();
            }
            
            if (freeSpaces == 0)
            {
                return new List<Request>();
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

            return allocatedRequests;
        }
    }
}