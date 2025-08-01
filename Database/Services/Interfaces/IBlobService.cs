using Azure.Storage.Blobs;
using Database.DBObjects.Enums;

namespace Database.Services.Interfaces;

public interface IBlobService
{
    BlobContainerClient GetBlobContainer(string containerName);
    BlobContainerClient GetBlobContainer(ContainerName containerName);
}
