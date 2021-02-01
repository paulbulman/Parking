namespace Parking.Data.UnitTests
{
    using System.Threading.Tasks;
    using Moq;
    using Xunit;

    public static class TriggerRepositoryTests
    {
        [Fact]
        public static async Task Returns_file_keys_from_raw_item_repository()
        {
            var keys = new[] {"key1", "key2"};

            var mockRawItemRepository = new Mock<IRawItemRepository>();
            mockRawItemRepository
                .Setup(r => r.GetTriggerFileKeys())
                .ReturnsAsync(keys);

            var triggerRepository = new TriggerRepository(mockRawItemRepository.Object);

            var result = await triggerRepository.GetKeys();

            Assert.Same(keys, result);
        }

        [Fact]
        public static async Task Passes_file_keys_to_raw_item_repository()
        {
            var keys = new[] { "key1", "key2" };

            var mockRawItemRepository = new Mock<IRawItemRepository>();
           
            var triggerRepository = new TriggerRepository(mockRawItemRepository.Object);

            await triggerRepository.DeleteKeys(keys);

            mockRawItemRepository.Verify(r => r.DeleteTriggerFiles(keys), Times.Once);
        }
    }
}