namespace Parking.Data
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

            return new Configuration(data.NearbyDistance, data.ShortLeadTimeSpaces, data.TotalSpaces);
        }

        // Class used with JsonSerializer.Deserialize
        // ReSharper disable once ClassNeverInstantiated.Local
        private class ConfigurationData
        {
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public decimal NearbyDistance { get; set; }

            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public int ShortLeadTimeSpaces { get; set; }

            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public int TotalSpaces { get; set; }
        }

    }
}