namespace Parking.Api.Controllers;

using System.Threading.Tasks;
using Business.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[Route("[controller]")]
[ApiController]
public class TriggersController(ITriggerRepository triggerRepository) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> PostAsync()
    {
        await triggerRepository.AddTrigger();

        return this.Ok();
    }
}