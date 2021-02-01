namespace Parking.Model
{
    using NodaTime;

    public class Schedule
    {
        public Schedule(ScheduledTaskType scheduledTaskType, Instant nextRunTime)
        {
            ScheduledTaskType = scheduledTaskType;
            NextRunTime = nextRunTime;
        }

        public ScheduledTaskType ScheduledTaskType { get; }

        public Instant NextRunTime { get; }
    }
}