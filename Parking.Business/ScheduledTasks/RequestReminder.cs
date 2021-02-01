namespace Parking.Business.ScheduledTasks
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Data;
    using Model;
    using NodaTime;

    public class RequestReminder : IScheduledTask
    {
        private readonly IDateCalculator dateCalculator;
        
        private readonly IEmailRepository emailRepository;
        
        private readonly IRequestRepository requestRepository;
        
        private readonly IUserRepository userRepository;

        public RequestReminder(
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

        public ScheduledTaskType ScheduledTaskType => ScheduledTaskType.RequestReminder;
        
        public async Task Run()
        {
            var nextWeeklyNotificationDates = dateCalculator.GetNextWeeklyNotificationDates();

            var requests = await requestRepository.GetRequests(
                nextWeeklyNotificationDates.First().PlusDays(-60),
                nextWeeklyNotificationDates.Last());

            var users = await userRepository.GetUsers();

            foreach (var user in users.Where(u =>
                IsActive(u, requests) && 
                !HasUpcomingActiveRequests(u, requests, nextWeeklyNotificationDates)))
            {
                await emailRepository.Send(
                    new EmailTemplates.RequestReminder(user, nextWeeklyNotificationDates));
            }
        }

        public Instant GetNextRunTime() =>
            this.dateCalculator.InitialInstant
                .InZone(DateCalculator.LondonTimeZone)
                .Date
                .Next(IsoDayOfWeek.Wednesday)
                .AtMidnight()
                .InZoneStrictly(DateCalculator.LondonTimeZone)
                .ToInstant();

        private static bool IsActive(User user, IReadOnlyCollection<Request> requests) =>
            requests.Any(r =>
                r.Status.IsActive() && 
                r.UserId == user.UserId);

        private static bool HasUpcomingActiveRequests(
            User user,
            IReadOnlyCollection<Request> requests,
            IReadOnlyCollection<LocalDate> nextWeeklyNotificationDates) =>
            requests.Any(r =>
                r.Status.IsActive() && 
                r.UserId == user.UserId && 
                nextWeeklyNotificationDates.Contains(r.Date));
    }
}