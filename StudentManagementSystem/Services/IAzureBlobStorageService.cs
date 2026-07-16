namespace StudentManagementSystem.Services;

public interface IAzureBlobStorageService
{
    Task<string> UploadProfileImageAsync(Stream imageStream, string fileName, string contentType);
    Task<bool> DeleteProfileImageAsync(string fileName);
    string? GetBlobSasUri(string? blobName, int expiresInMinutes = 30);
    Task<bool> ValidateImageAsync(Stream imageStream);
    string? GetBlobNameFromUrl(string? url);
}