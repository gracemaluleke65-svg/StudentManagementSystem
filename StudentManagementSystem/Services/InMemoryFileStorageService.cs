using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace StudentManagementSystem.Services
{
    public class InMemoryFileStorageService
    {
        private readonly Dictionary<string, byte[]> _fileStorage = new();
        private readonly IConfiguration _configuration;

        public InMemoryFileStorageService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> UploadProfileImageAsync(Stream imageStream, string fileName, string contentType)
        {
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    await imageStream.CopyToAsync(memoryStream);
                    var fileBytes = memoryStream.ToArray();

                    _fileStorage[fileName] = fileBytes;

                    return $"/images/{fileName}";
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error uploading file: {ex.Message}");
            }
        }

        public async Task<bool> DeleteProfileImageAsync(string fileName)
        {
            return _fileStorage.Remove(fileName);
        }

        public string GetBlobSasUri(string blobName)
        {
            return $"/images/{blobName}";
        }

        public async Task<bool> ValidateImageAsync(Stream imageStream)
        {
            try
            {
                if (imageStream.Length > 5 * 1024 * 1024)
                    return false;

                imageStream.Position = 0;
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

        public byte[] GetFileData(string fileName)
        {
            return _fileStorage.ContainsKey(fileName) ? _fileStorage[fileName] : null;
        }
    }
}