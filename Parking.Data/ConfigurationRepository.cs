namespace Parking.Data
{
    using System.Runtime.Serialization;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Aws;
    using Business.Data;
    using Model;

    public class ConfigurationRepository : IConfigurationRepository
    {
        private readonly IStorageProvider storageProvider;

        public ConfigurationRepository(IStorageProvider storageProvider) => this.storageProvider = storageProvider;

        public async Task<Configuration> GetConfiguration()
        {
            var rawData = await this.storageProvider.GetConfiguration();

            var data = JsonSerializer.Deserialize<ConfigurationData>(rawData);

            if (data == null)
            {
                throw new SerializationException("Could not deserialize raw configuration data.");
            }

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