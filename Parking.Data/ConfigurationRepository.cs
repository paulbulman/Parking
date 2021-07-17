namespace Parking.Data
{
    using System;
    using System.Threading.Tasks;
    using Aws;
    using Business.Data;
    using Model;

    public class ConfigurationRepository : IConfigurationRepository
    {
        private readonly IDatabaseProvider databaseProvider;

        public ConfigurationRepository(IDatabaseProvider databaseProvider) => this.databaseProvider = databaseProvider;

        public async Task<Configuration> GetConfiguration()
        {
            var rawData = await this.databaseProvider.GetConfiguration();

            if (rawData.Configuration == null)
            {
                throw new InvalidOperationException("No configuration data found.");
            }

            var nearbyDistance = decimal.Parse(rawData.Configuration["nearbyDistance"]);
            var shortLeadTimeSpaces = int.Parse(rawData.Configuration["shortLeadTimeSpaces"]);
            var totalSpaces = int.Parse(rawData.Configuration["totalSpaces"]);

            return new Configuration(nearbyDistance, shortLeadTimeSpaces, totalSpaces);
        }
    }
}