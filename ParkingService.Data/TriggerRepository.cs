namespace ParkingService.Data
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Business.Data;

    public class TriggerRepository : ITriggerRepository
    {
        private readonly IRawItemRepository rawItemRepository;

        public TriggerRepository(IRawItemRepository rawItemRepository) =>
            this.rawItemRepository = rawItemRepository;

        public async Task<IReadOnlyCollection<string>> GetKeys() =>
            await this.rawItemRepository.GetTriggerFileKeys();

        public async Task DeleteKeys(IReadOnlyCollection<string> keys) =>
            await this.rawItemRepository.DeleteTriggerFiles(keys);
    }
}