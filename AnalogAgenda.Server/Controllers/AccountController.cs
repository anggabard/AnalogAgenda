using AnalogAgenda.Server.Identity;
using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Entities;
using Database.Helpers;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AnalogAgenda.Server.Controllers;

[ApiController, Route("[controller]")]
public class AccountController(ITableService tables) : ControllerBase
{
    private readonly ITableService _tables = tables;

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto login)
    {
        var users = _tables.GetTable(TableName.Users);
        var result = await users.GetEntityIfExistsAsync<UserEntity>(
                         TableName.Users.PartitionKey(), login.Email.ToLowerInvariant());

        if (!result.HasValue) return Unauthorized("Bad creds");

        var user = result.Value!;

        if (!PasswordHasher.VerifyPassword(login.Password, user.PasswordHash))
            return Unauthorized("Bad creds");

        var claims = new[]
       {
            new Claim(ClaimTypes.NameIdentifier, user.RowKey),
            new Claim(ClaimTypes.Email,          user.RowKey),
            new Claim(ClaimTypes.Name,           user.Username)
        };
        var id = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(id),
            new AuthenticationProperties { IsPersistent = true });

        return Ok();
    }

    [HttpGet("whoAmI")]
    public IActionResult Me()
    {
        if (!User.IsAuthenticated())
            return Unauthorized();

        return Ok(new IdentityDto {
            Username = User.Name(),
            Email = User.Email()
        });
    }

    [Authorize]
    [HttpGet("isAuth")]
    public IActionResult IsAuth() => Ok();

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return Ok();
    }
}