namespace Parking.Business.Data
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Model;
    using NodaTime;

    public interface IRequestRepository
    {
        Task<IReadOnlyCollection<Request>> GetRequests(LocalDate firstDate, LocalDate lastDate);

        Task<IReadOnlyCollection<Request>> GetRequests(string userId, LocalDate firstDate, LocalDate lastDate);

        Task SaveRequests(IReadOnlyCollection<Request> requests, IReadOnlyCollection<User> users);
    }
}