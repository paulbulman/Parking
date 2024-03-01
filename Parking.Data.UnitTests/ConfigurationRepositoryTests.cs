namespace Parking.Data.UnitTests;

using System.Collections.Generic;
using System.Threading.Tasks;
using Aws;
using Moq;
using Xunit;

public static class ConfigurationRepositoryTests
{
    [Fact]
    public static async Task Converts_raw_data_to_configuration()
    {
        var mockDatabaseProvider = new Mock<IDatabaseProvider>(MockBehavior.Strict);

        var rawData = new Dictionary<string, string>
        {
            {"nearbyDistance", "3.5"},
            {"shortLeadTimeSpaces", "2"},
            {"totalSpaces", "9"}
        };

        mockDatabaseProvider
            .Setup(p => p.GetConfiguration())
            .ReturnsAsync(RawItem.CreateConfiguration(rawData));

        var configurationRepository = new ConfigurationRepository(mockDatabaseProvider.Object);

        var result = await configurationRepository.GetConfiguration();

        Assert.NotNull(result);

        Assert.Equal(3.5m, result.NearbyDistance);
        Assert.Equal(2, result.ShortLeadTimeSpaces);
        Assert.Equal(9, result.TotalSpaces);
    }
}