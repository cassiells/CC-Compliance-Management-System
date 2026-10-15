using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DoComply.Services
{
    public class CloudinaryDocumentService : ICloudinaryDocumentService
    {
        private readonly Cloudinary? _cloudinary;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<CloudinaryDocumentService> _logger;
        private readonly bool _isConfigured;

        public bool IsConfigured => _isConfigured;

        public CloudinaryDocumentService(
            IConfiguration configuration,
            IWebHostEnvironment environment,
            ILogger<CloudinaryDocumentService> logger)
        {
            _environment = environment;
            _logger = logger;

            var cloudName = configuration["Cloudinary:CloudName"];
            var apiKey = configuration["Cloudinary:ApiKey"];
            var apiSecret = configuration["Cloudinary:ApiSecret"];

            if (!string.IsNullOrWhiteSpace(cloudName) &&
                !string.IsNullOrWhiteSpace(apiKey) &&
                !string.IsNullOrWhiteSpace(apiSecret) &&
                !cloudName.Equals("your_cloud_name", StringComparison.OrdinalIgnoreCase) &&
                !apiKey.Equals("your_api_key", StringComparison.OrdinalIgnoreCase))
            {
                var account = new Account(cloudName, apiKey, apiSecret);
                _cloudinary = new Cloudinary(account)
                {
                    Api = { Secure = true }
                };
                _isConfigured = true;
                _logger.LogInformation("Cloudinary service initialized for cloud: {CloudName}", cloudName);
            }
            else
            {
                _isConfigured = false;
                _logger.LogWarning("Cloudinary credentials not configured in appsettings.json. Using local file storage fallback.");
            }
        }

        public async Task<CloudinaryUploadResult> UploadDocumentAsync(IFormFile file, string supplierFolder)
        {
            if (file == null || file.Length == 0)
            {
                return new CloudinaryUploadResult
                {
                    Success = false,
                    ErrorMessage = "No file was provided for upload."
                };
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant().TrimStart('.');
            var isImage = extension is "jpg" or "jpeg" or "png" or "webp" or "gif";
            var resourceType = isImage ? "image" : "raw";

            byte[] fileBytes;
            byte[] sha256Bytes;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                fileBytes = ms.ToArray();
            }

            using (var sha = SHA256.Create())
            {
                sha256Bytes = sha.ComputeHash(fileBytes);
            }

            // Upload to Cloudinary if credentials are provided
            if (_isConfigured && _cloudinary != null)
            {
                try
                {
                    var cleanFolderName = SanitizeFolder(supplierFolder);
                    var cleanBaseName = Path.GetFileNameWithoutExtension(file.FileName);
                    var publicId = $"{cleanBaseName}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}";

                    using var uploadStream = new MemoryStream(fileBytes);

                    if (isImage)
                    {
                        var uploadParams = new ImageUploadParams
                        {
                            File = new FileDescription(file.FileName, uploadStream),
                            Folder = $"docomply/{cleanFolderName}",
                            PublicId = publicId,
                            Overwrite = true,
                            UseFilename = true,
                            UniqueFilename = false
                        };

                        var result = await _cloudinary.UploadAsync(uploadParams);
                        if (result.Error != null)
                        {
                            throw new Exception(result.Error.Message);
                        }

                        return new CloudinaryUploadResult
                        {
                            Success = true,
                            StorageObjectKey = result.SecureUrl?.ToString() ?? result.PublicId,
                            SecureUrl = result.SecureUrl?.ToString(),
                            StorageFormat = result.Format ?? extension,
                            StorageResourceType = "image",
                            StorageVersion = long.TryParse(result.Version, out var ver) ? ver : 1,
                            FileSizeBytes = result.Bytes > 0 ? result.Bytes : file.Length,
                            FileSha256 = sha256Bytes
                        };
                    }
                    else
                    {
                        var uploadParams = new RawUploadParams
                        {
                            File = new FileDescription(file.FileName, uploadStream),
                            Folder = $"docomply/{cleanFolderName}",
                            PublicId = $"{publicId}.{extension}",
                            Overwrite = true,
                            UseFilename = true,
                            UniqueFilename = false
                        };

                        var result = await _cloudinary.UploadAsync(uploadParams);
                        if (result.Error != null)
                        {
                            throw new Exception(result.Error.Message);
                        }

                        return new CloudinaryUploadResult
                        {
                            Success = true,
                            StorageObjectKey = result.SecureUrl?.ToString() ?? result.PublicId,
                            SecureUrl = result.SecureUrl?.ToString(),
                            StorageFormat = result.Format ?? extension,
                            StorageResourceType = "raw",
                            StorageVersion = long.TryParse(result.Version, out var ver) ? ver : 1,
                            FileSizeBytes = result.Bytes > 0 ? result.Bytes : file.Length,
                            FileSha256 = sha256Bytes
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Cloudinary upload failed, falling back to local file storage: {Message}", ex.Message);
                }
            }

            // Local fallback
            var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadsDir = Path.Combine(webRoot, "uploads", "documents");
            if (!Directory.Exists(uploadsDir))
            {
                Directory.CreateDirectory(uploadsDir);
            }

            var safeName = $"{Guid.NewGuid():N}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(uploadsDir, safeName);

            await System.IO.File.WriteAllBytesAsync(filePath, fileBytes);

            return new CloudinaryUploadResult
            {
                Success = true,
                StorageObjectKey = $"/uploads/documents/{safeName}",
                SecureUrl = $"/uploads/documents/{safeName}",
                StorageFormat = extension,
                StorageResourceType = resourceType,
                StorageVersion = 1,
                FileSizeBytes = file.Length,
                FileSha256 = sha256Bytes
            };
        }

        private static string SanitizeFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder)) return "general";
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                folder = folder.Replace(c, '_');
            }
            return folder.Trim().Replace(' ', '_').ToLowerInvariant();
        }
    }
}
