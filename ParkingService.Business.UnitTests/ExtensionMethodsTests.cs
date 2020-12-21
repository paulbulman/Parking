namespace ParkingService.Business.UnitTests
{
    using NodaTime;
    using Xunit;

    public static class ExtensionMethodsTests
    {
        [Theory]
        [InlineData(2018, 11, 7, "07 Nov")]
        [InlineData(2019, 3, 2, "02 Mar")]
        public static void ToEmailDisplayString_returns_string_in_expected_format(
            int year,
            int month,
            int day,
            string expectedResult)
        {
            var localDate = new LocalDate(year, month, day);

            var actual = localDate.ToEmailDisplayString();
            
            Assert.Equal(expectedResult, actual);
        }
    }
}