using AnalogAgenda.Server.DTOs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AnalogAgenda.Server.Controllers;

[ApiController, Route("[controller]")]
public class AccountController : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto login)
    {
        //if(login.Password)
        //    return Unauthorized();

        var id = new ClaimsIdentity([new Claim(ClaimTypes.Name, "ApprovedUser")],
                                     CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                                      new ClaimsPrincipal(id),
                                      new AuthenticationProperties { IsPersistent = true });

        return Ok();
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return Ok();
    }
}