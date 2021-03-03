namespace Parking.Data.UnitTests
{
    using System.Threading.Tasks;
    using Aws;
    using Moq;
    using Xunit;

    public static class TriggerRepositoryTests
    {
        [Fact]
        public static async Task Calls_storage_provider_to_add_new_trigger()
        {
            var mockStorageProvider = new Mock<IStorageProvider>();

            var triggerRepository = new TriggerRepository(mockStorageProvider.Object);

            await triggerRepository.AddTrigger();

            mockStorageProvider.Verify(p => p.SaveTrigger(), Times.Once);
        }

        [Fact]
        public static async Task Returns_file_keys_from_storage_provider()
        {
            var keys = new[] {"key1", "key2"};

            var mockStorageProvider = new Mock<IStorageProvider>();
            mockStorageProvider
                .Setup(p => p.GetTriggerFileKeys())
                .ReturnsAsync(keys);

            var triggerRepository = new TriggerRepository(mockStorageProvider.Object);

            var result = await triggerRepository.GetKeys();

            Assert.Same(keys, result);
        }

        [Fact]
        public static async Task Passes_file_keys_to_storage_provider()
        {
            var keys = new[] { "key1", "key2" };

            var mockStorageProvider = new Mock<IStorageProvider>();
           
            var triggerRepository = new TriggerRepository(mockStorageProvider.Object);

            await triggerRepository.DeleteKeys(keys);

            mockStorageProvider.Verify(p => p.DeleteTriggerFiles(keys), Times.Once);
        }
    }
}