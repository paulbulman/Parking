﻿namespace Parking.Business
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Data;
    using EmailTemplates;
    using Microsoft.Extensions.Logging;
    using Model;
    using NodaTime;

    public class AllocationNotifier
    {
        private readonly ILogger<AllocationNotifier> logger;
        
        private readonly IDateCalculator dateCalculator;

        private readonly IEmailRepository emailRepository;

        private readonly IScheduleRepository scheduleRepository;

        private readonly IUserRepository userRepository;

        public AllocationNotifier(
            ILogger<AllocationNotifier> logger,
            IDateCalculator dateCalculator,
            IEmailRepository emailRepository,
            IScheduleRepository scheduleRepository,
            IUserRepository userRepository)
        {
            this.logger = logger;
            this.dateCalculator = dateCalculator;
            this.emailRepository = emailRepository;
            this.scheduleRepository = scheduleRepository;
            this.userRepository = userRepository;
        }

        public async Task Notify(IReadOnlyCollection<Request> updatedRequests)
        {
            if (!updatedRequests.Any())
            {
                return;
            }

            this.logger.LogDebug("Sending notifications for new requests.");

            var users = await this.userRepository.GetUsers();

            var schedules = await this.scheduleRepository.GetSchedules();

            var dailyNotificationSchedule =
                schedules.Single(s => s.ScheduledTaskType == ScheduledTaskType.DailyNotification);
            var weeklyNotificationSchedule =
                schedules.Single(s => s.ScheduledTaskType == ScheduledTaskType.WeeklyNotification);

            var datesToExclude = new List<LocalDate>();

            if (dateCalculator.ScheduleIsDue(dailyNotificationSchedule, within: Duration.FromMinutes(2)))
            {
                this.logger.LogDebug("Daily notification email is due soon. Excluding this date.");

                datesToExclude.Add(this.dateCalculator.GetNextWorkingDate());
            }

            if (dateCalculator.ScheduleIsDue(weeklyNotificationSchedule, within: Duration.FromMinutes(2)))
            {
                this.logger.LogDebug("Weekly notification email is due soon. Excluding these dates.");
                
                datesToExclude.AddRange(this.dateCalculator.GetWeeklyNotificationDates());
            }

            var requestsToNotify = updatedRequests.Where(r =>
                r.Status == RequestStatus.Allocated && !datesToExclude.Contains(r.Date));

            foreach (var requestsByUser in requestsToNotify.GroupBy(r => r.UserId))
            {
                var user = users.Single(u => u.UserId == requestsByUser.Key);
                var userRequests = requestsByUser.ToArray();

                var emailTemplate = userRequests.Length == 1
                    ? (IEmailTemplate)new SingleDayAllocationNotification(userRequests[0], user)
                    : new MultipleDayAllocationNotification(userRequests, user);

                await this.emailRepository.Send(emailTemplate);
            }
        }
    }
}