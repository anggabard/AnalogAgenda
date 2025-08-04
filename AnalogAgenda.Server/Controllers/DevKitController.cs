using AnalogAgenda.Server.Helpers;
using AutoMapper;
using Database.DBObjects;
using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Entities;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnalogAgenda.Server.Controllers;

[ApiController, Route("[controller]"), Authorize]
public class DevKitController(IMapper mapper, ITableService tables, IBlobService blobs) : ControllerBase
{
    private readonly IMapper _mapper = mapper;
    private readonly ITableService _tableService = tables;
    private readonly IBlobService _blobService = blobs;

    [HttpPost]
    public async Task<IActionResult> CreateNewKit([FromBody] DevKitDto dto)
    {
        var imageId = Constants.DefaultDevKitImageId;
        var devKitsTable = _tableService.GetTable(TableName.DevKits);
        var devKitsContainer = _blobService.GetBlobContainer(ContainerName.devkits);

        try
        {
            if (!string.IsNullOrEmpty(dto.ImageAsBase64))
            {
                imageId = Guid.NewGuid();
                await BlobImageHelper.UploadBase64ImageWithContentTypeAsync(devKitsContainer, dto.ImageAsBase64, imageId);
            }

            var entity = _mapper.Map<DevKitEntity>(dto);
            entity.ImageId = imageId;

            await devKitsTable.AddEntityAsync(entity);
        }
        catch (Exception ex)
        {
            if (imageId != Constants.DefaultDevKitImageId)
                await devKitsContainer.GetBlobClient(imageId.ToString()).DeleteIfExistsAsync();

            return UnprocessableEntity(ex.Message);
        }

        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> GetAllKits()
    {
        var entities = await _tableService.GetTableEntries<DevKitEntity>();

        var devKitsContainer = _blobService.GetBlobContainer(ContainerName.devkits);
        var results = await Task.WhenAll(
            entities.Select(async entity =>
                {
                    var dto = _mapper.Map<DevKitDto>(entity);
                    dto.ImageAsBase64 = await BlobImageHelper.DownloadImageAsBase64WithContentTypeAsync(devKitsContainer, entity.ImageId);
                    return dto;
                })
            );

        return Ok(results);
    }
}