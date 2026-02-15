using Database.DTOs;
using Database.Entities;
using Database.Services;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnalogAgenda.Server.Controllers;

[Route("api/[controller]"), ApiController, Authorize]
public class IdeaController(
    IDatabaseService databaseService,
    DtoConvertor dtoConvertor,
    EntityConvertor entityConvertor
) : ControllerBase
{
    private readonly IDatabaseService databaseService = databaseService;
    private readonly DtoConvertor dtoConvertor = dtoConvertor;
    private readonly EntityConvertor entityConvertor = entityConvertor;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var entities = await databaseService.GetAllAsync<IdeaEntity>();
        var results = entities.Select(dtoConvertor.ToDTO);
        return Ok(results);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var entity = await databaseService.GetByIdAsync<IdeaEntity>(id);
        if (entity == null)
            return NotFound($"No Idea found with Id: {id}");

        return Ok(dtoConvertor.ToDTO(entity));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] IdeaDto dto)
    {
        if (dto == null)
            return BadRequest("Invalid data.");

        var entity = entityConvertor.ToEntity(dto);
        await databaseService.AddAsync(entity);
        var createdDto = dtoConvertor.ToDTO(entity);
        return Created(string.Empty, createdDto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] IdeaDto dto)
    {
        if (dto == null)
            return BadRequest("Invalid data.");

        var existingEntity = await databaseService.GetByIdAsync<IdeaEntity>(id);
        if (existingEntity == null)
            return NotFound($"No Idea found with Id: {id}");

        existingEntity.Title = dto.Title;
        existingEntity.Description = dto.Description ?? string.Empty;
        existingEntity.UpdatedDate = DateTime.UtcNow;

        await databaseService.UpdateAsync(existingEntity);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var entity = await databaseService.GetByIdAsync<IdeaEntity>(id);
        if (entity == null)
            return NotFound($"No Idea found with Id: {id}");

        await databaseService.DeleteAsync(entity);
        return NoContent();
    }
}
