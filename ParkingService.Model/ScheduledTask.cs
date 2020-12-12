namespace ParkingService.Model
{
    using NodaTime;

    public class ScheduledTask
    {
        public ScheduledTask(ScheduledTaskType scheduledTaskType, Instant nextRunTime)
        {
            ScheduledTaskType = scheduledTaskType;
            NextRunTime = nextRunTime;
        }

        public ScheduledTaskType ScheduledTaskType { get; }

        public Instant NextRunTime { get; }
    }
}