namespace Parking.Api.Controllers
{
    using Microsoft.AspNetCore.Mvc;

    [Route("[controller]")]
    [ApiController]
    public class StatusController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetAsync() => this.Ok();
    }
}
