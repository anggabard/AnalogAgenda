using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnalogAgenda.Server.Controllers;

[ApiController, Route("[controller]")]
public class HomeController : ControllerBase
{
    [Authorize]
    [HttpGet("secret")]
    public IActionResult Secret() => Ok("🎉  super secret data");
}