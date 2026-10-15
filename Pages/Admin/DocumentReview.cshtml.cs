using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using DoComply.Models;
using DoComply.Services;

namespace DoComply.Pages.Admin
{
    public class DocumentReviewModel : PageModel
    {
        private readonly DoComplyContext _context;
        private readonly IBirTinVerificationService _birService;

        public DocumentReviewModel(DoComplyContext context, IBirTinVerificationService birService)
        {
            _context = context;
            _birService = birService;
        }

        [BindProperty(SupportsGet = true)]
        public long? Id { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Supplier { get; set; } = "ABC Office Supplies";

        [BindProperty(SupportsGet = true)]
        public string Doc { get; set; } = "Business Permit";

        public string FileName { get; set; } = "ABC_BusinessPermit_2026.pdf";
        public string SubmittedAt { get; set; } = "Today, 9:42 AM";
        public string ExpiryDateText { get; set; } = "December 31, 2026";
        public string Status { get; set; } = "Pending Verification";
        public string? StorageObjectKey { get; set; }
        public string FileSizeText { get; set; } = "1.8 MB";

        // BIR TIN Verification Properties
        public BirTinVerificationResult? BirResult { get; set; }
        public string SupplierTin { get; set; } = "245-891-304-000";
        public bool IsTaxDocument { get; set; }

        public async Task OnGetAsync()
        {
            if (Id.HasValue && Id.Value > 0)
            {
                var submission = await _context.DocumentSubmissions
                    .Include(s => s.Supplier)
                    .Include(s => s.Requirement)
                    .FirstOrDefaultAsync(s => s.DocumentSubmissionId == Id.Value);

                if (submission != null)
                {
                    Supplier = submission.Supplier.SupplierName;
                    Doc = submission.Requirement.RequirementName;
                    FileName = submission.OriginalFileName;
                    SubmittedAt = submission.SubmittedAtUtc.ToLocalTime().ToString("MMM dd, yyyy hh:mm tt");
                    ExpiryDateText = submission.ExpiryDate.HasValue ? submission.ExpiryDate.Value.ToString("MMMM dd, yyyy") : "No expiry";
                    Status = submission.Status;
                    StorageObjectKey = submission.StorageObjectKey;

                    if (submission.FileSizeBytes > 0)
                    {
                        if (submission.FileSizeBytes >= 1024 * 1024)
                        {
                            FileSizeText = (submission.FileSizeBytes / (1024.0 * 1024.0)).ToString("0.1") + " MB";
                        }
                        else
                        {
                            FileSizeText = (submission.FileSizeBytes / 1024.0).ToString("0") + " KB";
                        }
                    }
                }
            }

            // Assign standard TIN based on supplier
            SupplierTin = Supplier switch
            {
                "DEF Equipment Corp." => "198-442-701-000",
                "XYZ Training Services" => "310-776-852-000",
                "Global Logistics Inc." => "455-123-987-000",
                _ => "245-891-304-000"
            };

            IsTaxDocument = Doc.Contains("Tax", StringComparison.OrdinalIgnoreCase) || 
                            Doc.Contains("BIR", StringComparison.OrdinalIgnoreCase) || 
                            Doc.Contains("Permit", StringComparison.OrdinalIgnoreCase);

            // Execute BIR TIN verification
            BirResult = await _birService.VerifyTinAsync(SupplierTin, Supplier);
        }

        public async Task<IActionResult> OnPostVerifyTinAsync([FromForm] string tin, [FromForm] string? supplierName)
        {
            var result = await _birService.VerifyTinAsync(tin, supplierName ?? Supplier);
            return new JsonResult(result);
        }

        public async Task<IActionResult> OnPostApproveAsync([FromForm] long? submissionId)
        {
            var targetId = submissionId ?? Id;
            if (targetId.HasValue && targetId.Value > 0)
            {
                var submission = await _context.DocumentSubmissions
                    .Include(s => s.Supplier)
                    .Include(s => s.Requirement)
                    .FirstOrDefaultAsync(s => s.DocumentSubmissionId == targetId.Value);

                if (submission != null)
                {
                    var userDisplayName = HttpContext.Session.GetString("FullName") ?? "Administrator";
                    var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.RoleId == 2);
                    long adminId = adminUser?.UserId ?? submission.SubmittedByUserId;

                    submission.Status = "Valid";
                    submission.ReviewedAtUtc = DateTime.UtcNow;
                    submission.ReviewedByUserId = adminId;
                    submission.UpdatedAtUtc = DateTime.UtcNow;

                    _context.VerificationReviews.Add(new VerificationReview
                    {
                        DocumentSubmissionId = submission.DocumentSubmissionId,
                        AdministratorUserId = adminId,
                        Decision = "Approved",
                        ReviewedAtUtc = DateTime.UtcNow
                    });

                    _context.AuditLogs.Add(new AuditLog
                    {
                        OrganizationId = submission.Supplier.OrganizationId,
                        UserDisplayName = userDisplayName,
                        Action = $"Approved document ({submission.Requirement.RequirementName}) for {submission.Supplier.SupplierName}",
                        Module = "Compliance",
                        OccurredAtUtc = DateTime.UtcNow
                    });

                    _context.Notifications.Add(new Notification
                    {
                        UserId = submission.SubmittedByUserId,
                        SupplierId = submission.SupplierId,
                        DocumentSubmissionId = submission.DocumentSubmissionId,
                        NotificationType = "Approved",
                        Message = $"Your {submission.Requirement.RequirementName} was verified and approved by {userDisplayName}.",
                        IsRead = false,
                        CreatedAtUtc = DateTime.UtcNow
                    });

                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToPage("/Admin/VerificationQueue");
        }

        public async Task<IActionResult> OnPostRejectAsync([FromForm] long? submissionId, [FromForm] string reason)
        {
            var targetId = submissionId ?? Id;
            if (targetId.HasValue && targetId.Value > 0)
            {
                var submission = await _context.DocumentSubmissions
                    .Include(s => s.Supplier)
                    .Include(s => s.Requirement)
                    .FirstOrDefaultAsync(s => s.DocumentSubmissionId == targetId.Value);

                if (submission != null)
                {
                    var userDisplayName = HttpContext.Session.GetString("FullName") ?? "Administrator";
                    var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.RoleId == 2);
                    long adminId = adminUser?.UserId ?? submission.SubmittedByUserId;

                    submission.Status = "Rejected";
                    submission.RejectionReason = string.IsNullOrWhiteSpace(reason) ? "Document requirements not met" : reason.Trim();
                    submission.ReviewedAtUtc = DateTime.UtcNow;
                    submission.ReviewedByUserId = adminId;
                    submission.UpdatedAtUtc = DateTime.UtcNow;

                    _context.VerificationReviews.Add(new VerificationReview
                    {
                        DocumentSubmissionId = submission.DocumentSubmissionId,
                        AdministratorUserId = adminId,
                        Decision = "Rejected",
                        RejectionReason = submission.RejectionReason,
                        ReviewedAtUtc = DateTime.UtcNow
                    });

                    _context.AuditLogs.Add(new AuditLog
                    {
                        OrganizationId = submission.Supplier.OrganizationId,
                        UserDisplayName = userDisplayName,
                        Action = $"Rejected document ({submission.Requirement.RequirementName}) for {submission.Supplier.SupplierName}",
                        Module = "Compliance",
                        OccurredAtUtc = DateTime.UtcNow
                    });

                    _context.Notifications.Add(new Notification
                    {
                        UserId = submission.SubmittedByUserId,
                        SupplierId = submission.SupplierId,
                        DocumentSubmissionId = submission.DocumentSubmissionId,
                        NotificationType = "Rejected",
                        Message = $"Your {submission.Requirement.RequirementName} was rejected: {submission.RejectionReason}",
                        IsRead = false,
                        CreatedAtUtc = DateTime.UtcNow
                    });

                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToPage("/Admin/VerificationQueue");
        }
    }
}
