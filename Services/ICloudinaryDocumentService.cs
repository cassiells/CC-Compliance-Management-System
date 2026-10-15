using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace DoComply.Services
{
    public class CloudinaryUploadResult
    {
        public bool Success { get; set; }
        public string StorageObjectKey { get; set; } = string.Empty;
        public string? SecureUrl { get; set; }
        public string StorageFormat { get; set; } = string.Empty;
        public string StorageResourceType { get; set; } = "raw"; // 'raw' or 'image'
        public long? StorageVersion { get; set; }
        public long FileSizeBytes { get; set; }
        public byte[] FileSha256 { get; set; } = Array.Empty<byte>();
        public string? ErrorMessage { get; set; }
    }

    public interface ICloudinaryDocumentService
    {
        bool IsConfigured { get; }
        Task<CloudinaryUploadResult> UploadDocumentAsync(IFormFile file, string supplierFolder);
    }
}
