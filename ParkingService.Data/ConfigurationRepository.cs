namespace ParkingService.Data
{
    using System.Text.Json;
    using System.Threading.Tasks;
    using Business.Data;
    using Model;

    public class ConfigurationRepository : IConfigurationRepository
    {
        private readonly IRawItemRepository rawItemRepository;

        public ConfigurationRepository(IRawItemRepository rawItemRepository) => this.rawItemRepository = rawItemRepository;

        public async Task<Configuration> GetConfiguration()
        {
            var rawData = await rawItemRepository.GetConfiguration();

            var data = JsonSerializer.Deserialize<ConfigurationData>(rawData);

            return new Configuration(data.nearbyDistance, data.shortLeadTimeSpaces, data.totalSpaces);
        }

        // Various suppressions needed to use with JsonSerializer.Deserialize
        // ReSharper disable once ClassNeverInstantiated.Local
        private class ConfigurationData
        {
            // ReSharper disable once InconsistentNaming
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public decimal nearbyDistance { get; set; }

            // ReSharper disable once InconsistentNaming
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public int shortLeadTimeSpaces { get; set; }

            // ReSharper disable once InconsistentNaming
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public int totalSpaces { get; set; }
        }

    }
}