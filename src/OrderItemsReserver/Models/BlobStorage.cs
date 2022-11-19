using System.IO;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;

namespace OrderItemsReserver.Models;
public class BlobStorage
{
    private BlobContainerClient _client;

    public BlobStorage(IConfiguration config)
    {
        var connectionString = config["BlobStorageConnectionString"];
        var containerName = config["BlobStorageContainerName"];

        Guard.Against.NullOrEmpty(connectionString);
        Guard.Against.NullOrEmpty(containerName);

        Initialize(connectionString, containerName).GetAwaiter().GetResult();
    }

    public async Task Initialize(string connectionString, string fileContainerName)
    {
        var blobServiceClient = new BlobServiceClient(connectionString);
        _client = blobServiceClient.GetBlobContainerClient(fileContainerName);
        await _client.CreateIfNotExistsAsync();
    }

    public async Task SaveAsync(Stream file, string name)
    {
        var blobClient = _client.GetBlobClient(name);

        await blobClient.UploadAsync(file, true);
    }
}
