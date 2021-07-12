namespace Parking.TestHelpers
{
    using System.Collections.Generic;
    using Business;
    using Moq;
    using NodaTime;

    public static class CreateDateCalculator
    {
        public static IDateCalculator WithActiveDates(IReadOnlyCollection<LocalDate> activeDates)
        {
            var mockDateCalculator = new Mock<IDateCalculator>(MockBehavior.Strict);

            mockDateCalculator
                .Setup(d => d.GetActiveDates())
                .Returns(activeDates);

            return mockDateCalculator.Object;
        }

        public static IDateCalculator WithActiveDatesAndLongLeadTimeAllocationDates(
            IReadOnlyCollection<LocalDate> activeDates,
            IReadOnlyCollection<LocalDate> longLeadTimeAllocationDates)
        {
            var mockDateCalculator = new Mock<IDateCalculator>(MockBehavior.Strict);

            mockDateCalculator
                .Setup(d => d.GetActiveDates())
                .Returns(activeDates);
            mockDateCalculator
                .Setup(d => d.GetLongLeadTimeAllocationDates())
                .Returns(longLeadTimeAllocationDates);

            return mockDateCalculator.Object;
        }
    }
}