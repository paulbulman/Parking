namespace Parking.TestHelpers
{
    using System.Collections.Generic;
    using System.Linq;
    using Business;
    using Business.Data;
    using Model;
    using Moq;
    using NodaTime;

    public static class CreateReservationRepository
    {
        public static IReservationRepository WithReservations(
            IReadOnlyCollection<LocalDate> activeDates,
            IReadOnlyCollection<Reservation> reservations) =>
            MockWithReservations(activeDates, reservations).Object;

        public static Mock<IReservationRepository> MockWithReservations(IReadOnlyCollection<LocalDate> activeDates, IReadOnlyCollection<Reservation> reservations)
        {
            var mockReservationRepository = new Mock<IReservationRepository>(MockBehavior.Strict);

            mockReservationRepository
                .Setup(r => r.GetReservations(activeDates.ToDateInterval()))
                .ReturnsAsync(reservations);
            return mockReservationRepository;
        }
    }
}