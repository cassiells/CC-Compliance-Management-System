using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DoComply.Models;

namespace DoComply.Pages.Admin
{
    public class ExpiryMonitoringModel : PageModel
    {
        private readonly DoComplyContext _context;

        public ExpiryMonitoringModel(DoComplyContext context)
        {
            _context = context;
        }

        public List<ExpiryMonitoringItem> Items { get; set; } = new List<ExpiryMonitoringItem>();

        public async Task OnGetAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var dbDocs = await _context.DocumentSubmissions
                .Include(s => s.Supplier)
                .Include(s => s.Requirement)
                .Where(s => s.ExpiryDate != null)
                .OrderBy(s => s.ExpiryDate)
                .ToListAsync();

            if (dbDocs.Any())
            {
                Items = dbDocs.Select(d =>
                {
                    var exp = d.ExpiryDate!.Value;
                    int daysDiff = exp.DayNumber - today.DayNumber;

                    string timeRemaining;
                    string status;

                    if (daysDiff < 0)
                    {
                        timeRemaining = $"Expired {Math.Abs(daysDiff)} days ago";
                        status = "Expired";
                    }
                    else if (daysDiff <= 30)
                    {
                        timeRemaining = $"Expires in {daysDiff} days";
                        status = "Expiring Soon";
                    }
                    else
                    {
                        timeRemaining = $"Valid for {daysDiff} days";
                        status = "Valid";
                    }

                    return new ExpiryMonitoringItem
                    {
                        SupplierName = d.Supplier.SupplierName,
                        DocumentName = d.Requirement.RequirementName,
                        ExpiryDate = exp.ToString("MMM dd, yyyy"),
                        TimeRemaining = timeRemaining,
                        Status = status
                    };
                }).ToList();
            }
            else
            {
                Items = SampleDataFixture.GetSampleExpiryMonitoring();
            }
        }

        public async Task<IActionResult> OnPostNotifyAsync([FromForm] string supplierName, [FromForm] string documentName)
        {
            var org = await _context.Organizations.FirstOrDefaultAsync();
            int orgId = org?.OrganizationId ?? 1;

            var userDisplayName = HttpContext.Session.GetString("FullName") ?? "Administrator";
            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.SupplierName == supplierName);

            if (supplier != null)
            {
                var supUser = await _context.Users.FirstOrDefaultAsync(u => u.FullName == supplier.SupplierName && u.RoleId == 4)
                              ?? await _context.Users.FirstOrDefaultAsync(u => u.RoleId == 4);
                long targetUserId = supUser?.UserId ?? 4;

                _context.Notifications.Add(new Notification
                {
                    UserId = targetUserId,
                    SupplierId = supplier.SupplierId,
                    NotificationType = "Expiring",
                    Message = $"Urgent: Renewal required for {documentName}. Please upload the renewed credential.",
                    IsRead = false,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }

            _context.AuditLogs.Add(new AuditLog
            {
                OrganizationId = orgId,
                UserDisplayName = userDisplayName,
                Action = $"Dispatched automated renewal reminder to {supplierName} ({documentName})",
                Module = "Compliance",
                OccurredAtUtc = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}
