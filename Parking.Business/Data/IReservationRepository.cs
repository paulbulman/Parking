namespace Parking.Business.Data
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Model;
    using NodaTime;

    public interface IReservationRepository
    {
        Task<IReadOnlyCollection<Reservation>> GetReservations(LocalDate firstDate, LocalDate lastDate);

        Task SaveReservations(IReadOnlyCollection<Reservation> reservations, IReadOnlyCollection<User> users);

        Task DeleteReservations(User user, DateInterval dateInterval);
    }
}