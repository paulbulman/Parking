namespace Parking.Api.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Business;
    using Business.Data;
    using Json.Reservations;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Model;
    using NodaTime;
    using static Json.Calendar.Helpers;

    [Authorize(Policy = "IsTeamLeader")]
    [Route("[controller]")]
    [ApiController]
    public class ReservationsController : ControllerBase
    {
        private readonly IConfigurationRepository configurationRepository;
        
        private readonly IDateCalculator dateCalculator;
        
        private readonly IReservationRepository reservationRepository;
        
        private readonly IUserRepository userRepository;

        public ReservationsController(
            IConfigurationRepository configurationRepository,
            IDateCalculator dateCalculator,
            IReservationRepository reservationRepository,
            IUserRepository userRepository)
        {
            this.configurationRepository = configurationRepository;
            this.dateCalculator = dateCalculator;
            this.reservationRepository = reservationRepository;
            this.userRepository = userRepository;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ReservationsResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAsync()
        {
            var response = await this.GetReservations();

            return this.Ok(response);
        }

        [HttpPatch]
        [ProducesResponseType(typeof(ReservationsResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> PatchAsync([FromBody] ReservationsPatchRequest request)
        {
            var activeDates = this.dateCalculator.GetActiveDates();

            var reservations = request.Reservations
                .Where(r => activeDates.Contains(r.LocalDate))
                .SelectMany(CreateReservations)
                .ToList();

            var users = await this.userRepository.GetUsers();

            await this.reservationRepository.SaveReservations(reservations, users);

            var response = await this.GetReservations();

            return this.Ok(response);
        }

        private async Task<ReservationsResponse> GetReservations()
        {
            var configuration = await this.configurationRepository.GetConfiguration();

            var activeDates = this.dateCalculator.GetActiveDates();

            var reservations = await this.reservationRepository.GetReservations(activeDates.First(), activeDates.Last());

            var calendarData = activeDates.ToDictionary(
                d => d,
                d => CreateDailyData(d, reservations));

            var calendar = CreateCalendar(calendarData);

            var users = await this.userRepository.GetUsers();

            var reservationsUsers = users
                .OrderBy(u => u.LastName)
                .Select(u => new ReservationsUser(u.UserId, u.DisplayName()));

            return new ReservationsResponse(calendar, configuration.ShortLeadTimeSpaces, reservationsUsers);
        }

        private static ReservationsData CreateDailyData(
            LocalDate localDate,
            IReadOnlyCollection<Reservation> reservations)
        {
            var filteredReservations = reservations.Where(r => r.Date == localDate);

            return new ReservationsData(filteredReservations.Select(r => r.UserId));
        }

        private static IEnumerable<Reservation> CreateReservations(ReservationsPatchRequestDailyData dailyData) =>
            dailyData.UserIds.Select(userId => new Reservation(userId, dailyData.LocalDate));
    }
}
