namespace Parking.Business.Data
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Model;
    using NodaTime;

    public interface IGuestRequestRepository
    {
        Task<IReadOnlyCollection<GuestRequest>> GetGuestRequests(DateInterval dateInterval);

        Task SaveGuestRequest(GuestRequest guestRequest);

        Task SaveGuestRequests(IReadOnlyCollection<GuestRequest> guestRequests);

        Task UpdateGuestRequest(GuestRequest guestRequest);

        Task DeleteGuestRequest(LocalDate date, string id);
    }
}
