namespace Parking.Business.Data
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Model;
    using NodaTime;

    public interface IReservationRepository
    {
        Task<IReadOnlyCollection<Reservation>> GetReservations(LocalDate firstDate, LocalDate lastDate);
    }
}