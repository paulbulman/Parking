namespace Parking.Business
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Data;
    using Model;

    public class RequestUpdater
    {
        private readonly IAllocationCreator allocationCreator;

        private readonly IConfigurationRepository configurationRepository;

        private readonly IDateCalculator dateCalculator;

        private readonly IRequestRepository requestRepository;

        private readonly IReservationRepository reservationRepository;

        private readonly IUserRepository userRepository;

        public RequestUpdater(
            IAllocationCreator allocationCreator,
            IConfigurationRepository configurationRepository,
            IDateCalculator dateCalculator,
            IRequestRepository requestRepository,
            IReservationRepository reservationRepository,
            IUserRepository userRepository)
        {
            this.allocationCreator = allocationCreator;
            this.configurationRepository = configurationRepository;
            this.dateCalculator = dateCalculator;
            this.requestRepository = requestRepository;
            this.reservationRepository = reservationRepository;
            this.userRepository = userRepository;
        }

        public async Task<IReadOnlyCollection<Request>> Update()
        {
            var shortLeadTimeAllocationDates = this.dateCalculator.GetShortLeadTimeAllocationDates();
            var longLeadTimeAllocationDates = this.dateCalculator.GetLongLeadTimeAllocationDates();
            var allAllocationDates = shortLeadTimeAllocationDates.Concat(longLeadTimeAllocationDates).ToArray();

            var firstCacheDate = shortLeadTimeAllocationDates.First().PlusDays(-60);
            var lastCacheDate = longLeadTimeAllocationDates.Last();

            var requests = await this.requestRepository.GetRequests(firstCacheDate, lastCacheDate);
            var reservations = await this.reservationRepository.GetReservations(firstCacheDate, lastCacheDate);

            var users = await this.userRepository.GetUsers();
            var configuration = await this.configurationRepository.GetConfiguration();

            var newRequests = new List<Request>();
            var requestsCache = requests.ToList();

            var previouslyPendingRequests = requestsCache
                .Where(r => r.Status == RequestStatus.Pending && allAllocationDates.Contains(r.Date))
                .Select(r => new Request(r.UserId, r.Date, RequestStatus.Interrupted))
                .ToArray();

            UpdateRequests(newRequests, previouslyPendingRequests);
            UpdateRequests(requestsCache, previouslyPendingRequests);

            foreach (var allocationDate in shortLeadTimeAllocationDates)
            {
                var allocatedRequests = this.allocationCreator.Create(
                    allocationDate, requestsCache, reservations, users, configuration, LeadTimeType.Short);

                UpdateRequests(newRequests, allocatedRequests);
                UpdateRequests(requestsCache, allocatedRequests);
            }

            foreach (var allocationDate in longLeadTimeAllocationDates)
            {
                var allocatedRequests = this.allocationCreator.Create(
                    allocationDate, requestsCache, reservations, users, configuration, LeadTimeType.Long);

                UpdateRequests(newRequests, allocatedRequests);
                UpdateRequests(requestsCache, allocatedRequests);
            }

            await this.requestRepository.SaveRequests(newRequests);

            return newRequests;
        }

        private static void UpdateRequests(
            ICollection<Request> existingRequests,
            IEnumerable<Request> updatedRequests)
        {
            foreach (var updatedRequest in updatedRequests)
            {
                var previousExistingRequest = existingRequests.SingleOrDefault(r =>
                    r.UserId == updatedRequest.UserId && r.Date == updatedRequest.Date);

                if (previousExistingRequest != null)
                {
                    existingRequests.Remove(previousExistingRequest);
                }

                existingRequests.Add(updatedRequest);
            }
        }
    }
}