namespace ParkingService.Business.Data
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Model;
    using NodaTime;

    public interface IRequestRepository
    {
        Task<IReadOnlyCollection<Request>> GetRequests(LocalDate firstDate, LocalDate lastDate);

        Task SaveRequests(IReadOnlyCollection<Request> requests);
    }
}