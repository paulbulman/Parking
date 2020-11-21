namespace ParkingService.Data.UnitTests
{
    using Moq;
    using Xunit;

    public static class ConfigurationRepositoryTests
    {
        [Fact]
        public static async void Converts_raw_data_to_configuration()
        {
            var mockRawItemRepository = new Mock<IRawItemRepository>(MockBehavior.Strict);

            var rawData = "{\r\n  \"nearbyDistance\": 3.5,\r\n  \"shortLeadTimeSpaces\": 2,\r\n  \"totalSpaces\": 9\r\n}";
            mockRawItemRepository.Setup(r => r.GetConfiguration()).ReturnsAsync(rawData);
            
            var configurationRepository = new ConfigurationRepository(mockRawItemRepository.Object);

            var result = await configurationRepository.GetConfiguration();

            Assert.NotNull(result);

            Assert.Equal(3.5m, result.NearbyDistance);
            Assert.Equal(2, result.ShortLeadTimeSpaces);
            Assert.Equal(9, result.TotalSpaces);
        }
    }
}