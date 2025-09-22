using AnalogAgenda.Server.Helpers;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Configuration.Sections;
using Database.DBObjects;
using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Entities;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnalogAgenda.Server.Controllers;

[ApiController, Route("api/[controller]"), Authorize]
public class FilmController(Storage storageCfg, ITableService tablesService, IBlobService blobsService) : ControllerBase
{
    private readonly TableClient filmsTable = tablesService.GetTable(TableName.Films);
    private readonly BlobContainerClient filmsContainer = blobsService.GetBlobContainer(ContainerName.films);

    [HttpPost]
    public async Task<IActionResult> CreateNewFilm([FromBody] FilmDto dto)
    {
        var imageId = Constants.DefaultFilmImageId;

        try
        {
            if (!string.IsNullOrEmpty(dto.ImageBase64))
            {
                imageId = Guid.NewGuid();
                await BlobImageHelper.UploadBase64ImageWithContentTypeAsync(filmsContainer, dto.ImageBase64, imageId);
            }

            var entity = dto.ToEntity();
            entity.ImageId = imageId;

            await filmsTable.AddEntityAsync(entity);
        }
        catch (Exception ex)
        {
            if (imageId != Constants.DefaultFilmImageId)
                await filmsContainer.GetBlobClient(imageId.ToString()).DeleteIfExistsAsync();

            return UnprocessableEntity(ex.Message);
        }

        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> GetAllFilms()
    {
        var entities = await tablesService.GetTableEntriesAsync<FilmEntity>();
        var results = entities.Select(entity => entity.ToDTO(storageCfg.AccountName));

        return Ok(results);
    }

    [HttpGet("{rowKey}")]
    public async Task<IActionResult> GetFilmByRowKey(string rowKey)
    {
        var entity = await tablesService.GetTableEntryIfExistsAsync<FilmEntity>(rowKey);

        if (entity == null)
        {
            return NotFound($"No Film found with RowKey: {rowKey}");
        }

        return Ok(entity.ToDTO(storageCfg.AccountName));
    }

    [HttpPut("{rowKey}")]
    public async Task<IActionResult> UpdateFilm(string rowKey, [FromBody] FilmDto updateDto)
    {
        if (updateDto == null)
            return BadRequest("Invalid data.");

        var existingEntity = await tablesService.GetTableEntryIfExistsAsync<FilmEntity>(rowKey);
        if (existingEntity == null)
            return NotFound();

        var updatedEntity = updateDto.ToEntity();
        updatedEntity.CreatedDate = existingEntity.CreatedDate;

        var imageId = existingEntity.ImageId;
        if (!string.IsNullOrEmpty(updateDto.ImageBase64))
        {
            if (existingEntity.ImageId != Constants.DefaultFilmImageId)
            {
                await filmsContainer.DeleteBlobAsync(existingEntity.ImageId.ToString());
            }

            imageId = Guid.NewGuid();
            await BlobImageHelper.UploadBase64ImageWithContentTypeAsync(filmsContainer, updateDto.ImageBase64, imageId);
        }

        updatedEntity.ImageId = imageId;
        updatedEntity.UpdatedDate = DateTime.UtcNow;

        await filmsTable.UpdateEntityAsync(updatedEntity, existingEntity.ETag, TableUpdateMode.Replace);

        return NoContent();
    }

    [HttpDelete("{rowKey}")]
    public async Task<IActionResult> DeleteFilm(string rowKey)
    {
        var existingEntity = await tablesService.GetTableEntryIfExistsAsync<FilmEntity>(rowKey);
        if (existingEntity == null)
            return NotFound();

        if (existingEntity.ImageId != Constants.DefaultFilmImageId)
            await filmsContainer.DeleteBlobAsync(existingEntity.ImageId.ToString());

        await filmsTable.DeleteEntityAsync(existingEntity);
        return NoContent();
    }

}
