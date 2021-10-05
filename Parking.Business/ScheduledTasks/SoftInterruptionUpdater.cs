namespace Parking.Business.ScheduledTasks
{
    using System.Linq;
    using System.Threading.Tasks;
    using Data;
    using Model;
    using NodaTime;

    public class SoftInterruptionUpdater : IScheduledTask
    {
        private readonly IDateCalculator dateCalculator;
        private readonly IRequestRepository requestRepository;
        private readonly IUserRepository userRepository;

        public SoftInterruptionUpdater(
            IDateCalculator dateCalculator,
            IRequestRepository requestRepository,
            IUserRepository userRepository)
        {
            this.dateCalculator = dateCalculator;
            this.requestRepository = requestRepository;
            this.userRepository = userRepository;
        }

        public ScheduledTaskType ScheduledTaskType => ScheduledTaskType.SoftInterruptionUpdater;

        public async Task Run()
        {
            var nextWorkingDate = this.dateCalculator.GetNextWorkingDate();

            var requests = await this.requestRepository.GetRequests(nextWorkingDate, nextWorkingDate);

            var updatedRequests = requests
                .Where(r => r.Status == RequestStatus.Interrupted)
                .Select(r => new Request(r.UserId, r.Date, RequestStatus.SoftInterrupted))
                .ToArray();

            var users = await this.userRepository.GetUsers();

            await this.requestRepository.SaveRequests(updatedRequests, users);
        }

        public Instant GetNextRunTime() =>
            this.dateCalculator.GetNextWorkingDate()
                .At(new LocalTime(11, 2, 0))
                .InZoneStrictly(DateCalculator.LondonTimeZone)
                .ToInstant();
    }
}