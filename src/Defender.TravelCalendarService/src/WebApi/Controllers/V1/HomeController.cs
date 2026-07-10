using Microsoft.AspNetCore.Mvc;

namespace Defender.TravelCalendarService.WebApi.Controllers.V1;

[ApiController]
public class HomeController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "Healthy", service = "travel-calendar" });
}
