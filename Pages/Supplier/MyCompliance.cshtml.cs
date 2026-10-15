using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using DoComply.Models;
using DoComply.Services;

namespace DoComply.Pages.Supplier
{
    public class MyComplianceModel : PageModel
    {
        private readonly DoComplyContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ICloudinaryDocumentService _cloudinaryService;

        public MyComplianceModel(
            DoComplyContext context, 
            IWebHostEnvironment environment,
            ICloudinaryDocumentService cloudinaryService)
        {
            _context = context;
            _environment = environment;
            _cloudinaryService = cloudinaryService;
        }

        public string CompanyName { get; set; } = "ABC Office Supplies";
        public string OverallStatus { get; set; } = "NOT COMPLIANT";
        public string Reasons { get; set; } = "1 missing · 1 pending verification";

        public List<VwSupplierChecklist> ChecklistItems { get; set; } = new List<VwSupplierChecklist>();

        public async Task OnGetAsync()
        {
            var sessionName = HttpContext.Session.GetString("FullName");
            var sup = await _context.Suppliers.FirstOrDefaultAsync(s => 
                (sessionName != null && s.SupplierName.Contains(sessionName)) || 
                s.SupplierName == "ABC Office Supplies");

            if (sup != null)
            {
                CompanyName = sup.SupplierName;

                var comp = await _context.VwSupplierCompliances
                    .FirstOrDefaultAsync(c => c.SupplierId == sup.SupplierId);

                if (comp != null)
                {
                    OverallStatus = comp.ComplianceStatus?.ToUpperInvariant() == "COMPLIANT" ? "COMPLIANT" : "NOT COMPLIANT";
                    if (OverallStatus == "COMPLIANT")
                    {
                        Reasons = $"All {comp.ValidCount ?? 5} documents verified & valid";
                    }
                    else
                    {
                        var parts = new List<string>();
                        if ((comp.ExpiredCount ?? 0) > 0) parts.Add($"{comp.ExpiredCount} expired");
                        if ((comp.RejectedCount ?? 0) > 0) parts.Add($"{comp.RejectedCount} rejected");
                        if ((comp.MissingCount ?? 0) > 0) parts.Add($"{comp.MissingCount} missing");
                        if ((comp.PendingVerificationCount ?? 0) > 0) parts.Add($"{comp.PendingVerificationCount} pending verification");
                        Reasons = parts.Any() ? string.Join(" · ", parts) : "Compliance incomplete";
                    }
                }

                ChecklistItems = await _context.VwSupplierChecklists
                    .Where(c => c.SupplierId == sup.SupplierId)
                    .OrderBy(c => c.RequirementName)
                    .ToListAsync();
            }
        }

        public async Task<IActionResult> OnPostUploadAsync([FromForm] string requirementName, [FromForm] string? expiryDate, [FromForm] IFormFile? documentFile)
        {
            if (string.IsNullOrWhiteSpace(requirementName))
            {
                return RedirectToPage();
            }

            var sup = await _context.Suppliers.FirstOrDefaultAsync(s => s.SupplierName == "ABC Office Supplies");
            if (sup == null)
            {
                return RedirectToPage();
            }

            var req = await _context.Requirements.FirstOrDefaultAsync(r => r.RequirementName == requirementName.Trim());
            if (req == null)
            {
                return RedirectToPage();
            }

            var supplierUser = await _context.Users.FirstOrDefaultAsync(u => u.RoleId == 4);
            long userId = supplierUser?.UserId ?? 1;

            DateOnly? parsedExpiry = null;
            if (!string.IsNullOrWhiteSpace(expiryDate) && DateOnly.TryParse(expiryDate, out var exp))
            {
                parsedExpiry = exp;
            }

            var cleanName = req.RequirementName.Replace(" ", "");
            string originalFileName = $"ABC_{cleanName}_2026.pdf";
            string storageObjectKey = $"docs/abc_{cleanName.ToLower()}.pdf";
            string storageFormat = "pdf";
            string storageResourceType = "raw";
            long? storageVersion = 1;
            long fileSizeBytes = 1048576;
            byte[] sha256Bytes = new byte[32];
            string contentType = "application/pdf";

            if (documentFile != null && documentFile.Length > 0)
            {
                originalFileName = Path.GetFileName(documentFile.FileName);
                contentType = string.IsNullOrWhiteSpace(documentFile.ContentType) ? "application/pdf" : documentFile.ContentType;

                var uploadResult = await _cloudinaryService.UploadDocumentAsync(documentFile, sup.SupplierName);
                if (uploadResult.Success)
                {
                    storageObjectKey = uploadResult.StorageObjectKey;
                    fileSizeBytes = uploadResult.FileSizeBytes;
                    sha256Bytes = uploadResult.FileSha256;
                    storageFormat = uploadResult.StorageFormat;
                    storageResourceType = uploadResult.StorageResourceType;
                    storageVersion = uploadResult.StorageVersion;
                }
            }

            var existingSubmission = await _context.DocumentSubmissions
                .FirstOrDefaultAsync(d => d.SupplierId == sup.SupplierId && d.RequirementId == req.RequirementId);

            DocumentSubmission targetSubmission;
            if (existingSubmission != null)
            {
                existingSubmission.VersionNumber += 1;
                existingSubmission.OriginalFileName = originalFileName;
                existingSubmission.StorageObjectKey = storageObjectKey;
                existingSubmission.StorageFormat = storageFormat;
                existingSubmission.StorageVersion = storageVersion;
                existingSubmission.FileSizeBytes = fileSizeBytes;
                existingSubmission.FileSha256 = sha256Bytes;
                existingSubmission.ContentType = contentType;
                existingSubmission.StorageProvider = "Cloudinary";
                existingSubmission.StorageResourceType = storageResourceType;
                existingSubmission.Status = "Pending Verification";
                existingSubmission.ExpiryDate = parsedExpiry;
                existingSubmission.SubmittedAtUtc = DateTime.UtcNow;
                existingSubmission.ReviewedAtUtc = null;
                existingSubmission.ReviewedByUserId = null;
                existingSubmission.RejectionReason = null;
                existingSubmission.UpdatedAtUtc = DateTime.UtcNow;
                targetSubmission = existingSubmission;
            }
            else
            {
                var submission = new DocumentSubmission
                {
                    SupplierId = sup.SupplierId,
                    RequirementId = req.RequirementId,
                    VersionNumber = 1,
                    OriginalFileName = originalFileName,
                    StorageObjectKey = storageObjectKey,
                    StorageFormat = storageFormat,
                    StorageVersion = storageVersion,
                    StorageProvider = "Cloudinary",
                    StorageResourceType = storageResourceType,
                    ContentType = contentType,
                    FileSizeBytes = fileSizeBytes,
                    FileSha256 = sha256Bytes,
                    ExpiryDate = parsedExpiry,
                    Status = "Pending Verification",
                    SubmittedByUserId = userId,
                    SubmittedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                };
                _context.DocumentSubmissions.Add(submission);
                targetSubmission = submission;
            }

            _context.AuditLogs.Add(new AuditLog
            {
                OrganizationId = sup.OrganizationId,
                UserDisplayName = $"Supplier ({sup.SupplierName})",
                Action = $"Uploaded document ({req.RequirementName})",
                Module = "Compliance",
                OccurredAtUtc = DateTime.UtcNow
            });

            // Notify Admin
            var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.RoleId == 2);
            if (adminUser != null)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = adminUser.UserId,
                    SupplierId = sup.SupplierId,
                    DocumentSubmissionId = targetSubmission.DocumentSubmissionId,
                    NotificationType = "info",
                    Message = $"{sup.SupplierName} submitted {req.RequirementName} for verification.",
                    IsRead = false,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}
