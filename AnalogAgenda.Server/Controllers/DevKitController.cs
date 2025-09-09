using AnalogAgenda.Server.Helpers;
using AutoMapper;
using Configuration.Sections;
using Database.DBObjects;
using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Entities;
using Database.Helpers;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnalogAgenda.Server.Controllers;

[ApiController, Route("[controller]"), Authorize]
public class DevKitController(IMapper mapper, Storage storageCfg, ITableService tablesService, IBlobService blobsService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateNewKit([FromBody] DevKitDto dto)
    {
        var imageId = Constants.DefaultDevKitImageId;
        var devKitsTable = tablesService.GetTable(TableName.DevKits);
        var devKitsContainer = blobsService.GetBlobContainer(ContainerName.devkits);

        try
        {
            if (!string.IsNullOrEmpty(dto.Image))
            {
                imageId = Guid.NewGuid();
                await BlobImageHelper.UploadBase64ImageWithContentTypeAsync(devKitsContainer, dto.Image, imageId);
            }

            var entity = mapper.Map<DevKitEntity>(dto);
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
        var entities = await tablesService.GetTableEntries<DevKitEntity>();
        var results =
            entities.Select(entity =>
                {
                    var dto = mapper.Map<DevKitDto>(entity);
                    dto.Image = BlobUrlHelper.GetUrlFromImageImageInfo(storageCfg.AccountName, ContainerName.devkits.ToString(), entity.ImageId);

                    return dto;
                });

        return Ok(results);
    }
}