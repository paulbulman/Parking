namespace Parking.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Aws;
    using Business.Data;

    public class TriggerRepository : ITriggerRepository
    {
        private readonly IDatabaseProvider databaseProvider;

        public TriggerRepository(IDatabaseProvider databaseProvider) =>
            this.databaseProvider = databaseProvider;

        public async Task AddTrigger() =>
            await this.databaseProvider.SaveItem(
                RawItem.CreateTrigger(Guid.NewGuid().ToString()));

        public async Task<IReadOnlyCollection<string>> GetKeys()
        {
            var rawItems = await this.databaseProvider.GetTriggers();

            return rawItems.Select(r => r.SortKey).ToArray();
        }

        public async Task DeleteKeys(IReadOnlyCollection<string> keys) =>
            await this.databaseProvider.DeleteItems(
                keys.Select(RawItem.CreateTrigger));
    }
}