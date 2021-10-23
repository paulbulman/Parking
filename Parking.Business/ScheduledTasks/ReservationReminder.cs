namespace Parking.Business.ScheduledTasks
{
    using System.Linq;
    using System.Threading.Tasks;
    using Data;
    using Model;
    using NodaTime;

    public class ReservationReminder : IScheduledTask
    {
        private readonly IDateCalculator dateCalculator;

        private readonly IEmailRepository emailRepository;

        private readonly IReservationRepository reservationRepository;

        private readonly IUserRepository userRepository;

        public ReservationReminder(
            IDateCalculator dateCalculator,
            IEmailRepository emailRepository,
            IReservationRepository reservationRepository,
            IUserRepository userRepository)
        {
            this.dateCalculator = dateCalculator;
            this.emailRepository = emailRepository;
            this.reservationRepository = reservationRepository;
            this.userRepository = userRepository;
        }

        public ScheduledTaskType ScheduledTaskType => ScheduledTaskType.ReservationReminder;

        public async Task Run()
        {
            var nextWorkingDate = dateCalculator.GetNextWorkingDate();

            var reservations = await reservationRepository.GetReservations(nextWorkingDate, nextWorkingDate);

            if (!reservations.Any())
            {
                var teamLeaderUsers = await userRepository.GetTeamLeaderUsers();

                foreach (var teamLeaderUser in teamLeaderUsers.Where(u => u.ReservationReminderEnabled))
                {
                    await emailRepository.Send(
                        new EmailTemplates.ReservationReminder(teamLeaderUser, nextWorkingDate));
                }
            }
        }

        public Instant GetNextRunTime() =>
            this.dateCalculator.GetNextWorkingDate()
                .At(new LocalTime(10, 0, 0))
                .InZoneStrictly(DateCalculator.LondonTimeZone)
                .ToInstant();
    }
}