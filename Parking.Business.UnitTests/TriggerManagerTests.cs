namespace Parking.Business.UnitTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data;
using Moq;
using Xunit;

public static class TriggerManagerTests
{
    [Fact]
    public static async Task Indicates_tasks_should_run_when_trigger_keys_exist()
    {
        var mockTriggerRepository = new Mock<ITriggerRepository>();
        mockTriggerRepository
            .Setup(r => r.GetKeys())
            .ReturnsAsync(new[] { "key" });

        var triggerManager = new TriggerManager(mockTriggerRepository.Object);

        var result = await triggerManager.ShouldRun();

        Assert.True(result);
    }

    [Fact]
    public static async Task Indicates_tasks_should_not_run_when_no_trigger_keys_exist()
    {
        var mockTriggerRepository = new Mock<ITriggerRepository>();
        mockTriggerRepository
            .Setup(r => r.GetKeys())
            .ReturnsAsync(new List<string>());

        var triggerManager = new TriggerManager(mockTriggerRepository.Object);

        var result = await triggerManager.ShouldRun();

        Assert.False(result);
    }

    [Fact]
    public static async Task Clears_triggers_when_run_is_complete()
    {
        var mockTriggerRepository = new Mock<ITriggerRepository>();
        mockTriggerRepository
            .SetupSequence(r => r.GetKeys())
            .ReturnsAsync(new[] { "key1", "key2" })
            .ReturnsAsync(new List<string>())
            .ReturnsAsync(new[] { "key3", "key4" });

        var triggerManager = new TriggerManager(mockTriggerRepository.Object);

        var result1 = await triggerManager.ShouldRun();
        await triggerManager.MarkComplete();
            
        var result2 = await triggerManager.ShouldRun();
        await triggerManager.MarkComplete();
            
        var result3 = await triggerManager.ShouldRun();
        await triggerManager.MarkComplete();

        Assert.True(result1);
        Assert.False(result2);
        Assert.True(result3);
    }

    [Fact]
    public static async Task Requests_to_delete_trigger_files_when_run_is_complete()
    {
        var mockTriggerRepository = new Mock<ITriggerRepository>();
        mockTriggerRepository
            .Setup(r => r.GetKeys())
            .ReturnsAsync(new[] { "key1", "key2" });

        var triggerManager = new TriggerManager(mockTriggerRepository.Object);

        await triggerManager.ShouldRun();
        await triggerManager.MarkComplete();

        mockTriggerRepository.Verify(
            r => r.DeleteKeys(
                It.Is<IReadOnlyCollection<string>>(k => k.Contains("key1") && k.Contains("key2"))),
            Times.Once);
    }

    [Fact]
    public static async Task Throws_exception_when_multiple_simultaneous_runs_are_attempted()
    {
        var mockTriggerRepository = new Mock<ITriggerRepository>();
        mockTriggerRepository
            .Setup(r => r.GetKeys())
            .ReturnsAsync(new[] { "key" });

        var triggerManager = new TriggerManager(mockTriggerRepository.Object);

        await triggerManager.ShouldRun();

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await triggerManager.ShouldRun());
    }
}