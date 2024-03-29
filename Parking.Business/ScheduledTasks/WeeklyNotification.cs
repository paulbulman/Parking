﻿namespace Parking.Business.ScheduledTasks
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

            var requests = await requestRepository.GetRequests(notificationDates.ToDateInterval());

            var users = await userRepository.GetUsers();

            foreach (var userId in requests.Where(r => r.Status.IsRequested()).Select(r => r.UserId).Distinct())
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
                .At(new LocalTime(0, 2, 0))
                .InZoneStrictly(DateCalculator.LondonTimeZone)
                .ToInstant();
    }
}