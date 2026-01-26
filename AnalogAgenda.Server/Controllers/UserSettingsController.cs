using AnalogAgenda.Server.Identity;
using Database.DTOs;
using Database.Entities;
using Database.Services;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnalogAgenda.Server.Controllers;

[Route("api/[controller]"), ApiController, Authorize]
public class UserSettingsController(
    IDatabaseService databaseService,
    DtoConvertor dtoConvertor
) : ControllerBase
{
    private readonly IDatabaseService databaseService = databaseService;
    private readonly DtoConvertor dtoConvertor = dtoConvertor;

    [HttpGet]
    public async Task<IActionResult> GetUserSettings()
    {
        var currentUserId = User.Id();
        if (string.IsNullOrEmpty(currentUserId))
            return Unauthorized();

        var userSettingsList = await databaseService.GetAllAsync<UserSettingsEntity>(us => us.UserId == currentUserId);
        var userSettings = userSettingsList.FirstOrDefault();

        if (userSettings == null)
            return NotFound("UserSettings not found for current user.");

        return Ok(dtoConvertor.ToDTO(userSettings));
    }

    [HttpPatch]
    public async Task<IActionResult> UpdateUserSettings([FromBody] UserSettingsDto dto)
    {
        if (dto == null)
            return BadRequest("Invalid data.");

        var currentUserId = User.Id();
        if (string.IsNullOrEmpty(currentUserId))
            return Unauthorized();

        // Ensure user can only update their own settings
        if (dto.UserId != currentUserId)
            return Forbid("You can only update your own settings.");

        var userSettingsList = await databaseService.GetAllAsync<UserSettingsEntity>(us => us.UserId == currentUserId);
        var existingSettings = userSettingsList.FirstOrDefault();

        if (existingSettings == null)
            return NotFound("UserSettings not found for current user.");

        // Update existing settings
        existingSettings.IsSubscribed = dto.IsSubscribed;
        existingSettings.CurrentFilmId = dto.CurrentFilmId;
        existingSettings.TableView = dto.TableView;
        existingSettings.EntitiesPerPage = dto.EntitiesPerPage;
        existingSettings.UpdatedDate = DateTime.UtcNow;

        await databaseService.UpdateAsync(existingSettings);

        return Ok(dtoConvertor.ToDTO(existingSettings));
    }

    [HttpGet("subscribed-users")]
    public async Task<IActionResult> GetSubscribedUsers()
    {
        var subscribedUserSettings = await databaseService.GetAllAsync<UserSettingsEntity>(us => us.IsSubscribed);
        var subscribedUserIds = subscribedUserSettings.Select(us => us.UserId).ToList();

        var users = await databaseService.GetAllAsync<UserEntity>(u => subscribedUserIds.Contains(u.Id));
        var result = users.Select(u => new { Username = u.Name }).ToList();

        return Ok(result);
    }
}

