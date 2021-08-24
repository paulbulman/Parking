namespace Parking.Business.ScheduledTasks
{
    using System.Linq;
    using System.Threading.Tasks;
    using Data;
    using Model;
    using NodaTime;

    public class DailyNotification : IScheduledTask
    {
        private readonly IDateCalculator dateCalculator;

        private readonly IEmailRepository emailRepository;

        private readonly IRequestRepository requestRepository;

        private readonly IUserRepository userRepository;

        public DailyNotification(
            IDateCalculator dateCalculator,
            IEmailRepository emailRepository,
            IRequestRepository requestRepository,
            IUserRepository userRepository)
        {
            this.dateCalculator = dateCalculator;
            this.emailRepository = emailRepository;
            this.requestRepository = requestRepository;
            this.userRepository = userRepository;
        }

        public ScheduledTaskType ScheduledTaskType => ScheduledTaskType.DailyNotification;

        public async Task Run()
        {
            var nextWorkingDate = dateCalculator.GetNextWorkingDate();

            var requests = await requestRepository.GetRequests(nextWorkingDate, nextWorkingDate);

            var users = await userRepository.GetUsers();

            foreach (var userId in requests.Where(r => r.Status.IsRequested()).Select(r => r.UserId))
            {
                var user = users.Single(u => u.UserId == userId);

                await emailRepository.Send(
                    new EmailTemplates.DailyNotification(requests, user, nextWorkingDate));
            }
        }

        public Instant GetNextRunTime() =>
            this.dateCalculator.GetNextWorkingDate()
                .At(new LocalTime(11, 0, 0))
                .InZoneStrictly(DateCalculator.LondonTimeZone)
                .ToInstant();
    }
}