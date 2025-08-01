using AnalogAgenda.Server.Helpers;
using AutoMapper;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Database.DBObjects;
using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Entities;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace AnalogAgenda.Server.Controllers;

[ApiController, Route("[controller]"), Authorize]
public class DevKitController(IMapper mapper, ITableService tables, IBlobService blobs) : ControllerBase
{
    private readonly IMapper _mapper = mapper;
    private readonly ITableService _tables = tables;
    private readonly IBlobService _blobs = blobs;

    [HttpPost]
    public async Task<IActionResult> CreateNewKit([FromBody] DevKitDto dto)
    {
        var imageId = Constants.DefaultDevKitImageId;
        var devKitsTable = _tables.GetTable(TableName.DevKits);
        var devKitsContainer = _blobs.GetBlobContainer(ContainerName.devkits);

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
            return Ok(entity.RowKey);
        }
        catch (Exception ex)
        {
            if (imageId != Constants.DefaultDevKitImageId)
                await devKitsContainer.GetBlobClient(imageId.ToString()).DeleteIfExistsAsync();

            return UnprocessableEntity(ex.Message);
        }
    }
}