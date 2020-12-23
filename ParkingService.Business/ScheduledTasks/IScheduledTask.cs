namespace ParkingService.Business.ScheduledTasks
{
    using System.Threading.Tasks;
    using Model;

    public interface IScheduledTask
    {
        ScheduledTaskType ScheduledTaskType { get; }

        Task Run();
    }
}