namespace Parking.TestHelpers
{
    using System.Collections.Generic;
    using Business.Data;
    using Model;
    using Moq;
    using NodaTime;

    public class RequestRepositoryBuilder
    {
        private readonly Mock<IRequestRepository> mockRequestRepository = new Mock<IRequestRepository>();

        public RequestRepositoryBuilder WithGetRequests(
            string userId,
            DateInterval dateInterval,
            IReadOnlyCollection<Request> requests)
        {
            this.mockRequestRepository
                .Setup(r => r.GetRequests(userId, dateInterval))
                .ReturnsAsync(requests);

            return this;
        }

        public RequestRepositoryBuilder WithGetRequests(
            DateInterval dateInterval,
            IReadOnlyCollection<Request> requests)
        {
            this.mockRequestRepository
                .Setup(r => r.GetRequests(dateInterval))
                .ReturnsAsync(requests);

            return this;
        }

        public IRequestRepository Build() => this.mockRequestRepository.Object;

        public Mock<IRequestRepository> BuildMock() => this.mockRequestRepository;
    }
}