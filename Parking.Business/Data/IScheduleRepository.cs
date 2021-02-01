namespace Parking.Business.Data
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Model;

    public interface IScheduleRepository
    {
        Task<IReadOnlyCollection<Schedule>> GetSchedules();
        
        Task UpdateSchedule(Schedule schedule);
    }
}