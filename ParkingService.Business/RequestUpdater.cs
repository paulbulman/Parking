namespace ParkingService.Business
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

            var firstDate = shortLeadTimeAllocationDates.First().PlusDays(-60);
            var lastDate = longLeadTimeAllocationDates.Last();

            var requests = await this.requestRepository.GetRequests(firstDate, lastDate);
            var reservations = await this.reservationRepository.GetReservations(firstDate, lastDate);

            var users = await this.userRepository.GetUsers();
            var configuration = await this.configurationRepository.GetConfiguration();

            var updatedRequests = new List<Request>();
            var cumulativeRequests = requests.ToList();

            foreach (var allocationDate in shortLeadTimeAllocationDates)
            {
                var allocatedRequests = this.allocationCreator.Create(
                    allocationDate, cumulativeRequests, reservations, users, configuration, LeadTimeType.Short);

                updatedRequests.AddRange(allocatedRequests);

                foreach (var allocatedRequest in allocatedRequests)
                {
                    ReplaceStaleRequest(cumulativeRequests, allocatedRequest);
                }
            }

            foreach (var allocationDate in longLeadTimeAllocationDates)
            {
                var allocatedRequests = this.allocationCreator.Create(
                    allocationDate, cumulativeRequests, reservations, users, configuration, LeadTimeType.Long);

                updatedRequests.AddRange(allocatedRequests);

                foreach (var allocatedRequest in allocatedRequests)
                {
                    ReplaceStaleRequest(cumulativeRequests, allocatedRequest);
                }
            }

            await this.requestRepository.SaveRequests(updatedRequests);

            return updatedRequests;
        }

        private static void ReplaceStaleRequest(ICollection<Request> cumulativeRequests, Request allocatedRequest)
        {
            var previousExistingRequest = cumulativeRequests.Single(r =>
                r.UserId == allocatedRequest.UserId && r.Date == allocatedRequest.Date);

            cumulativeRequests.Remove(previousExistingRequest);
            cumulativeRequests.Add(allocatedRequest);
        }
    }
}