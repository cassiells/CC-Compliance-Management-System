using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using DoComply.Models;

namespace DoComply.Pages.Admin
{
    public class ProcurementDetailsModel : PageModel
    {
        private readonly DoComplyContext _context;

        public ProcurementDetailsModel(DoComplyContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; } = 101;

        [BindProperty(SupportsGet = true)]
        public string Supplier { get; set; } = "ABC Office Supplies";

        [BindProperty(SupportsGet = true)]
        public string RequestItemTitle { get; set; } = "Office Supplies";

        [BindProperty(SupportsGet = true)]
        public bool Compliant { get; set; } = true;

        public string Purpose { get; set; } = "Office materials for the department including high-yield toner cartridges, recycled printer paper, and stationery bundles for Q4 operations.";
        public string DateSubmitted { get; set; } = "Sept. 20, 2026";
        public string RequestedBy { get; set; } = "Leah Ramos (Staff-HR)";
        public string Status { get; set; } = "Pending Approval";

        public async Task OnGetAsync()
        {
            if (Id > 0)
            {
                var req = await _context.ProcurementRequests
                    .Include(p => p.Supplier)
                    .Include(p => p.RequestedByUser)
                    .FirstOrDefaultAsync(p => p.ProcurementRequestId == Id);

                if (req != null)
                {
                    Supplier = req.Supplier.SupplierName;
                    RequestItemTitle = req.RequestName;
                    Purpose = req.Purpose;
                    DateSubmitted = req.CreatedAtUtc.ToLocalTime().ToString("MMM dd, yyyy");
                    RequestedBy = $"{req.RequestedByUser.FullName} (Staff)";
                    Status = req.Status;

                    var compliance = await _context.VwSupplierCompliances
                        .FirstOrDefaultAsync(c => c.SupplierId == req.SupplierId);

                    Compliant = compliance?.ComplianceStatus?.Equals("Compliant", StringComparison.OrdinalIgnoreCase) == true;
                }
            }
        }

        public async Task<IActionResult> OnPostApproveAsync([FromForm] long? requestId)
        {
            long targetId = requestId ?? Id;
            var req = await _context.ProcurementRequests
                .Include(p => p.Supplier)
                .FirstOrDefaultAsync(p => p.ProcurementRequestId == targetId);

            if (req != null)
            {
                var userDisplayName = HttpContext.Session.GetString("FullName") ?? "Administrator";
                var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.RoleId == 2);
                long adminId = adminUser?.UserId ?? 1;

                req.Status = "Approved";
                req.ApprovedAtUtc = DateTime.UtcNow;
                req.ApprovedByUserId = adminId;

                _context.AuditLogs.Add(new AuditLog
                {
                    OrganizationId = req.OrganizationId,
                    UserDisplayName = userDisplayName,
                    Action = $"Approved procurement request ({req.RequestName}) for {req.Supplier.SupplierName}",
                    Module = "Procurement",
                    OccurredAtUtc = DateTime.UtcNow
                });

                _context.Notifications.Add(new Notification
                {
                    UserId = req.RequestedByUserId,
                    SupplierId = req.SupplierId,
                    NotificationType = "Approved",
                    Message = $"Your procurement request for '{req.RequestName}' has been approved by {userDisplayName}.",
                    IsRead = false,
                    CreatedAtUtc = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
            }

            return RedirectToPage("/Admin/Procurement");
        }
    }
}
