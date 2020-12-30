namespace ParkingService.Business.ScheduledTasks
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Data;
    using Model;

    public class ScheduledTaskRunner
    {
        private readonly IDateCalculator dateCalculator;
        
        private readonly IReadOnlyCollection<IScheduledTask> scheduledTasks;
        
        private readonly IScheduleRepository scheduleRepository;

        public ScheduledTaskRunner(
            IDateCalculator dateCalculator,
            IReadOnlyCollection<IScheduledTask> scheduledTasks,
            IScheduleRepository scheduleRepository)
        {
            this.dateCalculator = dateCalculator;
            this.scheduledTasks = scheduledTasks;
            this.scheduleRepository = scheduleRepository;
        }

        public async Task RunScheduledTasks()
        {
            var schedules = await this.scheduleRepository.GetSchedules();

            foreach (var schedule in schedules.Where(ScheduleIsDue))
            {
                var scheduledTask = this.scheduledTasks.Single(t => t.ScheduledTaskType == schedule.ScheduledTaskType);

                await scheduledTask.Run();
            }
        }

        private bool ScheduleIsDue(Schedule schedule) => schedule.NextRunTime <= this.dateCalculator.InitialInstant;
    }
}