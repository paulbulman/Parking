namespace Parking.Api.Controllers
{
    using System.Linq;
    using System.Threading.Tasks;
    using Business;
    using Business.Data;
    using Microsoft.AspNetCore.Mvc;

    [Route("[controller]")]
    [ApiController]
    public class RequestsController : ControllerBase
    {
        private readonly IDateCalculator dateCalculator;
        
        private readonly IRequestRepository requestRepository;

        public RequestsController(
            IDateCalculator dateCalculator,
            IRequestRepository requestRepository)
        {
            this.dateCalculator = dateCalculator;
            this.requestRepository = requestRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync()
        {
            var activeDates = this.dateCalculator.GetActiveDates();

            var requests = await this.requestRepository.GetRequests(activeDates.First(), activeDates.Last());

            return this.Ok(requests);
        }
    }
}
