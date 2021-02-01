namespace Parking.Business.UnitTests
{
    using System.Linq;
    using Model;
    using NodaTime;
    using Xunit;

    public static class ExtensionMethodsTests
    {
        [Theory]
        [InlineData(2018, 11, 7, "Wed 07 Nov")]
        [InlineData(2019, 3, 2, "Sat 02 Mar")]
        public static void ToEmailDisplayString_formats_LocalDate(
            int year,
            int month,
            int day,
            string expectedResult)
        {
            var localDate = new LocalDate(year, month, day);

            var actual = localDate.ToEmailDisplayString();
            
            Assert.Equal(expectedResult, actual);
        }

        [Theory]
        [InlineData(2018, 11, 6, 2019, 1, 2, "Tue 06 Nov - Wed 02 Jan")]
        [InlineData(2019, 4, 3, 2019, 4, 3, "Wed 03 Apr - Wed 03 Apr")]
        public static void ToEmailDisplayString_formats_DateInterval(
            int startYear,
            int startMonth,
            int startDay,
            int endYear,
            int endMonth,
            int endDay,
            string expectedResult)
        {
            var dateInterval = new DateInterval(
                new LocalDate(startYear, startMonth, startDay),
                new LocalDate(endYear, endMonth, endDay));

            Assert.Equal(expectedResult, dateInterval.ToEmailDisplayString());
            Assert.Equal(expectedResult, dateInterval.Reverse().ToEmailDisplayString());
        }

        [Theory]
        [InlineData(RequestStatus.Requested, true)]
        [InlineData(RequestStatus.Allocated, true)]
        [InlineData(RequestStatus.Cancelled, false)]
        public static void IsActive_returns_true_for_requested_and_allocated_statuses(RequestStatus requestStatus, bool expectedResult)
        {
            var actual = requestStatus.IsActive();
            
            Assert.Equal(expectedResult, actual);
        }
    }
}