namespace Parking.TestHelpers
{
    using System.Collections.Generic;
    using Business.Data;
    using Model;
    using Moq;
    using NodaTime;

    public static class CreateRequestRepository
    {
        public static IRequestRepository WithRequests(IReadOnlyCollection<Request> requests)
        {
            var mockRequestRepository = new Mock<IRequestRepository>(MockBehavior.Strict);

            mockRequestRepository
                .Setup(r => r.GetRequests(It.IsAny<LocalDate>(), It.IsAny<LocalDate>()))
                .ReturnsAsync(requests);

            return mockRequestRepository.Object;
        }
    }
}