namespace Parking.Data.UnitTests;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aws;
using Moq;
using Xunit;

public static class TriggerRepositoryTests
{
    [Fact]
    public static async Task Calls_database_provider_to_add_new_trigger()
    {
        var mockDatabaseProvider = new Mock<IDatabaseProvider>();

        var triggerRepository = new TriggerRepository(mockDatabaseProvider.Object);

        await triggerRepository.AddTrigger();

        mockDatabaseProvider.Verify(
            p => p.SaveItem(It.Is<RawItem>(r =>
                r.PrimaryKey == "TRIGGER" && 
                r.SortKey.Length == 36 &&
                r.Trigger == r.SortKey)),
            Times.Once);
    }

    [Fact]
    public static async Task Returns_trigger_keys_from_database_provider()
    {
        var keys = new[] {"key1", "key2"};

        var triggers = keys.Select(RawItem.CreateTrigger).ToArray();

        var mockDatabaseProvider = new Mock<IDatabaseProvider>();
        mockDatabaseProvider
            .Setup(p => p.GetTriggers())
            .ReturnsAsync(triggers);

        var triggerRepository = new TriggerRepository(mockDatabaseProvider.Object);

        var result = await triggerRepository.GetKeys();

        Assert.Equal(keys, result);
    }

    [Fact]
    public static async Task Passes_trigger_keys_to_database_provider()
    {
        var keys = new[] { "key1", "key2" };

        var mockDatabaseProvider = new Mock<IDatabaseProvider>();
           
        var triggerRepository = new TriggerRepository(mockDatabaseProvider.Object);

        await triggerRepository.DeleteKeys(keys);

        mockDatabaseProvider.Verify(
            p => p.DeleteItems(It.Is<IEnumerable<RawItem>>(c => CheckTriggers(keys, c.ToArray()))), 
            Times.Once);
    }

    private static bool CheckTriggers(
        IReadOnlyCollection<string> expectedKeys,
        IReadOnlyCollection<RawItem> actualRawItems) => 
        actualRawItems.All(r => r.PrimaryKey == "TRIGGER") &&
        actualRawItems.Select(r => r.SortKey).SequenceEqual(expectedKeys);
}