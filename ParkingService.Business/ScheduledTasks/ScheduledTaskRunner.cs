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

            var dueTasks = schedules
                .Where(ScheduleIsDue)
                .Select(GetScheduledTask)
                .ToArray();

            foreach (var dueTask in dueTasks)
            {
                await dueTask.Run();
                
                var updatedSchedule = new Schedule(dueTask.ScheduledTaskType, dueTask.GetNextRunTime());
                
                await this.scheduleRepository.UpdateSchedule(updatedSchedule);
            }
        }

        private bool ScheduleIsDue(Schedule schedule) => schedule.NextRunTime <= this.dateCalculator.InitialInstant;
        
        private IScheduledTask GetScheduledTask(Schedule schedule) =>
            this.scheduledTasks.Single(task => task.ScheduledTaskType == schedule.ScheduledTaskType);
    }
}