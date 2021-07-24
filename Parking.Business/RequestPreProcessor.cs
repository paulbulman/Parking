namespace Parking.Business
{
    using System.Linq;
    using System.Threading.Tasks;
    using Data;
    using Model;

    public class RequestPreProcessor
    {
        private readonly IDateCalculator dateCalculator;
        private readonly IRequestRepository requestRepository;

        public RequestPreProcessor(IDateCalculator dateCalculator, IRequestRepository requestRepository)
        {
            this.dateCalculator = dateCalculator;
            this.requestRepository = requestRepository;
        }

        public async Task Update()
        {
            var shortLeadTimeAllocationDates = this.dateCalculator.GetShortLeadTimeAllocationDates();
            var longLeadTimeAllocationDates = this.dateCalculator.GetLongLeadTimeAllocationDates();

            var requests = await this.requestRepository.GetRequests(
                shortLeadTimeAllocationDates.First(), 
                longLeadTimeAllocationDates.Last());

            var updatedRequests = requests
                .Where(r => r.Status == RequestStatus.Pending)
                .Select(r => new Request(r.UserId, r.Date, RequestStatus.Interrupted))
                .ToArray();

            await this.requestRepository.SaveRequests(updatedRequests);
        }
    }
}