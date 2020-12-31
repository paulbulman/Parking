namespace ParkingService.Business.ScheduledTasks
{
    using System.Linq;
    using System.Threading.Tasks;
    using Data;
    using Model;
    using NodaTime;

    public class WeeklyNotification : IScheduledTask
    {
        private readonly IDateCalculator dateCalculator;
        
        private readonly IEmailRepository emailRepository;
        
        private readonly IRequestRepository requestRepository;
        
        private readonly IUserRepository userRepository;

        public WeeklyNotification(
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

        public ScheduledTaskType ScheduledTaskType => ScheduledTaskType.WeeklyNotification;

        public async Task Run()
        {
            var notificationDates = dateCalculator.GetWeeklyNotificationDates();

            var requests = await requestRepository.GetRequests(notificationDates.First(), notificationDates.Last());

            var users = await userRepository.GetUsers();

            foreach (var userId in requests.Where(r => r.Status.IsActive()).Select(r => r.UserId).Distinct())
            {
                var user = users.Single(u => u.UserId == userId);

                await this.emailRepository.Send(
                    new EmailTemplates.WeeklyNotification(requests, user, notificationDates));
            }
        }

        public Instant GetNextRunTime() =>
            this.dateCalculator.InitialInstant
                .InZone(DateCalculator.LondonTimeZone)
                .Date
                .Next(IsoDayOfWeek.Thursday)
                .AtMidnight()
                .InZoneStrictly(DateCalculator.LondonTimeZone)
                .ToInstant();
    }
}