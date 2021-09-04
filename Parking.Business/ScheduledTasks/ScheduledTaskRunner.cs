namespace Parking.Business.ScheduledTasks
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Data;
    using Model;

    public class ScheduledTaskRunner
    {
        private readonly IDateCalculator dateCalculator;
        
        private readonly IEnumerable<IScheduledTask> scheduledTasks;
        
        private readonly IScheduleRepository scheduleRepository;

        public ScheduledTaskRunner(
            IDateCalculator dateCalculator,
            IEnumerable<IScheduledTask> scheduledTasks,
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
                .Where(s => this.dateCalculator.ScheduleIsDue(s))
                .Select(GetScheduledTask)
                .ToArray();

            foreach (var dueTask in dueTasks)
            {
                await dueTask.Run();
                
                var updatedSchedule = new Schedule(dueTask.ScheduledTaskType, dueTask.GetNextRunTime());
                
                await this.scheduleRepository.UpdateSchedule(updatedSchedule);
            }
        }

        private IScheduledTask GetScheduledTask(Schedule schedule) =>
            this.scheduledTasks.Single(task => task.ScheduledTaskType == schedule.ScheduledTaskType);
    }
}