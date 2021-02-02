namespace Parking.Api.Controllers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Business;
    using Business.Data;
    using Microsoft.AspNetCore.Mvc;
    using Model;

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
        public async Task<IEnumerable<Request>> GetAsync()
        {
            var activeDates = this.dateCalculator.GetActiveDates();

            return await this.requestRepository.GetRequests(activeDates.First(), activeDates.Last());
        }
    }
}
