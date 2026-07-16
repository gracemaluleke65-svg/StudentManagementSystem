using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;  // ADDED
using SixLabors.ImageSharp.Processing;

namespace StudentManagementSystem.Services;

public class AzureBlobStorageService : IAzureBlobStorageService
{
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<AzureBlobStorageService> _logger;

    public AzureBlobStorageService(IConfiguration configuration, ILogger<AzureBlobStorageService> logger)
    {
        _logger = logger;

        var connectionString = configuration["BlobStorage:ConnectionString"]
            ?? throw new ArgumentNullException("BlobStorage:ConnectionString");
        var containerName = configuration["BlobStorage:ContainerName"]
            ?? throw new ArgumentNullException("BlobStorage:ContainerName");

        var blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
    }

    public async Task InitializeAsync()
    {
        await _containerClient.CreateIfNotExistsAsync(PublicAccessType.None);
        _logger.LogInformation("Blob storage container initialized: {ContainerName}", _containerClient.Name);
    }

    public async Task<string> UploadProfileImageAsync(Stream imageStream, string fileName, string contentType)
    {
        try
        {
            using var image = await Image.LoadAsync(imageStream);

            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(400, 400),
                Mode = ResizeMode.Max
            }));

            var outputStream = new MemoryStream();
            await image.SaveAsJpegAsync(outputStream, new JpegEncoder  // FIXED: Use JpegEncoder directly
            {
                Quality = 85
            });
            outputStream.Position = 0;

            var blobClient = _containerClient.GetBlobClient(fileName);
            var blobHttpHeaders = new BlobHttpHeaders
            {
                ContentType = "image/jpeg",
                ContentDisposition = $"inline; filename=\"{fileName}\""
            };

            await blobClient.UploadAsync(outputStream, new BlobUploadOptions
            {
                HttpHeaders = blobHttpHeaders,
                Metadata = new Dictionary<string, string>
                {
                    { "uploadedAt", DateTime.UtcNow.ToString("O") }
                }
            });

            _logger.LogInformation("Uploaded image: {FileName}", fileName);
            return blobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image {FileName}", fileName);
            throw;
        }
    }

    public async Task<bool> DeleteProfileImageAsync(string fileName)
    {
        try
        {
            var blobClient = _containerClient.GetBlobClient(fileName);
            var response = await blobClient.DeleteIfExistsAsync();
            if (response.Value)
                _logger.LogInformation("Deleted image: {FileName}", fileName);
            return response.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image {FileName}", fileName);
            return false;
        }
    }

    public string? GetBlobSasUri(string? blobName, int expiresInMinutes = 30)
    {
        if (string.IsNullOrEmpty(blobName)) return null;

        try
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            if (!blobClient.Exists()) return null;

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerClient.Name,
                BlobName = blobName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(expiresInMinutes),
                Protocol = SasProtocol.Https,
                ContentType = "image/jpeg",
                ContentDisposition = "inline"
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            return blobClient.GenerateSasUri(sasBuilder).ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating SAS for {BlobName}", blobName);
            return null;
        }
    }

    public async Task<bool> ValidateImageAsync(Stream imageStream)
    {
        if (imageStream == null || imageStream.Length == 0) return false;
        if (imageStream.Length > 5 * 1024 * 1024) return false;

        try
        {
            imageStream.Position = 0;
            using var image = await Image.LoadAsync(imageStream);

            var format = image.Metadata.DecodedImageFormat?.Name.ToLowerInvariant();
            if (format != "jpeg" && format != "jpg" && format != "png")
                return false;

            if (image.Width > 5000 || image.Height > 5000)
                return false;

            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            imageStream.Position = 0;
        }
    }

    public string? GetBlobNameFromUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return null;

        try
        {
            var uri = new Uri(url);
            return Path.GetFileName(uri.LocalPath);
        }
        catch
        {
            return null;
        }
    }
}