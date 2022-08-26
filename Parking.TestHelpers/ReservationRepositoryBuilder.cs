namespace Parking.TestHelpers
{
    using System.Collections.Generic;
    using Business.Data;
    using Model;
    using Moq;
    using NodaTime;

    public class ReservationRepositoryBuilder
    {
        private readonly Mock<IReservationRepository> mockReservationRepository = new Mock<IReservationRepository>();

        public ReservationRepositoryBuilder WithGetReservations(
            DateInterval dateInterval,
            IReadOnlyCollection<Reservation> reservations)
        {
            this.mockReservationRepository
                .Setup(r => r.GetReservations(dateInterval))
                .ReturnsAsync(reservations);

            return this;
        }

        public IReservationRepository Build() => this.mockReservationRepository.Object;

        public Mock<IReservationRepository> BuildMock() => this.mockReservationRepository;
    }
}