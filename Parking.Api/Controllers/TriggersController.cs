namespace Parking.Api.Controllers;

using System.Threading.Tasks;
using Business.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[Route("[controller]")]
[ApiController]
public class TriggersController : ControllerBase
{
    private readonly ITriggerRepository triggerRepository;

    public TriggersController(ITriggerRepository triggerRepository) =>
        this.triggerRepository = triggerRepository;

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> PostAsync()
    {
        await this.triggerRepository.AddTrigger();

        return this.Ok();
    }
}