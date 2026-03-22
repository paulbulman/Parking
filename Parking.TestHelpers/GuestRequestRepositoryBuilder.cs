namespace Parking.TestHelpers
{
    using System.Collections.Generic;
    using Business.Data;
    using Model;
    using Moq;
    using NodaTime;

    public class GuestRequestRepositoryBuilder
    {
        private readonly Mock<IGuestRequestRepository> mockGuestRequestRepository = new Mock<IGuestRequestRepository>();

        public GuestRequestRepositoryBuilder WithGetGuestRequests(
            DateInterval dateInterval,
            IReadOnlyCollection<GuestRequest> guestRequests)
        {
            this.mockGuestRequestRepository
                .Setup(r => r.GetGuestRequests(dateInterval))
                .ReturnsAsync(guestRequests);

            return this;
        }

        public IGuestRequestRepository Build() => this.mockGuestRequestRepository.Object;

        public Mock<IGuestRequestRepository> BuildMock() => this.mockGuestRequestRepository;
    }
}
