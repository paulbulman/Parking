namespace Parking.Business.Data
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Model;
    using NodaTime;

    public interface IRequestRepository
    {
        Task<IReadOnlyCollection<Request>> GetRequests(DateInterval dateInterval);

        Task<IReadOnlyCollection<Request>> GetRequests(string userId, DateInterval dateInterval);

        Task SaveRequests(IReadOnlyCollection<Request> requests);
    }
}