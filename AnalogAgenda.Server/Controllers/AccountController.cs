using AnalogAgenda.Server.Identity;
using Database.Data;
using Database.DTOs;
using Database.Entities;
using Database.Helpers;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AnalogAgenda.Server.Controllers;

[ApiController, Route("api/[controller]")]
public class AccountController(IDatabaseService database, AnalogAgendaDbContext dbContext) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto login)
    {
        var users = await database.GetAllAsync<UserEntity>(user => user.Email == login.Email.ToLowerInvariant());

        if (users.Count == 0) return Unauthorized("Bad creds");

        var user = users.Single();

        if (!PasswordHasher.VerifyPassword(login.Password, user.PasswordHash))
            return Unauthorized("Bad creds");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email,          user.Email),
            new Claim(ClaimTypes.Name,           user.Name)
        };
        var id = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(id),
            new AuthenticationProperties { IsPersistent = true });

        return Ok();
    }

    [Authorize]
    [HttpPost("changePassword")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        // Load entity without tracking to avoid conflicts
        var existingEntity = await dbContext.Set<UserEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == User.Id());

        if (existingEntity == null) return Problem("Something went terribly wrong.");

        if (!PasswordHasher.VerifyPassword(dto.OldPassword, existingEntity.PasswordHash))
            return Unauthorized("Bad creds");

        // Create updated entity
        var updatedEntity = new UserEntity
        {
            Id = existingEntity.Id,
            CreatedDate = existingEntity.CreatedDate,
            UpdatedDate = DateTime.UtcNow,
            Name = existingEntity.Name,
            Email = existingEntity.Email,
            PasswordHash = PasswordHasher.HashPassword(dto.NewPassword),
            IsSubscraibed = existingEntity.IsSubscraibed
        };

        // Attach and update
        dbContext.Set<UserEntity>().Attach(updatedEntity);
        dbContext.Entry(updatedEntity).State = EntityState.Modified;
        
        await dbContext.SaveChangesAsync();

        return Ok();
    }

    [HttpGet("whoAmI")]
    public IActionResult Me()
    {
        if (!User.IsAuthenticated())
            return Unauthorized();

        return Ok(new IdentityDto
        {
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