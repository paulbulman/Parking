namespace Parking.TestHelpers
{
    using System.Collections.Generic;
    using System.Linq;
    using Business.Data;
    using Model;
    using Moq;
    using NodaTime;

    public static class CreateReservationRepository
    {
        public static IReservationRepository WithReservations(
            IReadOnlyCollection<LocalDate> activeDates,
            IReadOnlyCollection<Reservation> reservations)
        {
            var mockReservationRepository = new Mock<IReservationRepository>(MockBehavior.Strict);

            mockReservationRepository
                .Setup(r => r.GetReservations(activeDates.First(), activeDates.Last()))
                .ReturnsAsync(reservations);

            return mockReservationRepository.Object;
        }
    }
}