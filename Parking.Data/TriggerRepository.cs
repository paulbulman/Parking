namespace Parking.Data
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Aws;
    using Business.Data;

    public class TriggerRepository : ITriggerRepository
    {
        private readonly IStorageProvider storageProvider;

        public TriggerRepository(IStorageProvider storageProvider) =>
            this.storageProvider = storageProvider;

        public async Task AddTrigger() =>
            await this.storageProvider.SaveTrigger();

        public async Task<IReadOnlyCollection<string>> GetKeys() =>
            await this.storageProvider.GetTriggerFileKeys();

        public async Task DeleteKeys(IReadOnlyCollection<string> keys) =>
            await this.storageProvider.DeleteTriggerFiles(keys);
    }
}