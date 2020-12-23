namespace ParkingService.Business.ScheduledTasks
{
    using System.Linq;
    using System.Threading.Tasks;
    using Data;
    using Model;

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
        
        public ScheduledTaskType ScheduledTaskType => ScheduledTaskType.DailySummary;
        
        public async Task Run()
        {
            var nextWorkingDate = dateCalculator.GetNextWorkingDate();

            var requests = await requestRepository.GetRequests(nextWorkingDate, nextWorkingDate);

            var users = await userRepository.GetUsers();

            foreach (var userId in requests.Where(r => r.Status.IsActive()).Select(r => r.UserId))
            {
                var user = users.Single(u => u.UserId == userId);

                await emailRepository.Send(
                    new EmailTemplates.DailyNotification(requests, user, nextWorkingDate));
            }
        }
    }
}