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
    public class SuppliersModel : PageModel
    {
        private readonly DoComplyContext _context;

        public SuppliersModel(DoComplyContext context)
        {
            _context = context;
        }

        public class SupplierDisplayItem
        {
            public long Id { get; set; }
            public string Name { get; set; } = "";
            public string ContactPerson { get; set; } = "";
            public string Email { get; set; } = "";
            public string Status { get; set; } = "Active";
            public string ComplianceStatus { get; set; } = "COMPLIANT";
            public string ComplianceReasons { get; set; } = "All required documents valid";
        }

        public List<SupplierDisplayItem> Suppliers { get; set; } = new List<SupplierDisplayItem>();

        public async Task OnGetAsync()
        {
            var dbSuppliers = await _context.Suppliers
                .OrderBy(s => s.SupplierName)
                .ToListAsync();

            var complianceMap = await _context.VwSupplierCompliances
                .ToDictionaryAsync(c => c.SupplierId, c => c);

            var list = new List<SupplierDisplayItem>();

            foreach (var s in dbSuppliers)
            {
                string compStatus = "NOT COMPLIANT";
                string compReasons = "No compliance records";

                if (complianceMap.TryGetValue(s.SupplierId, out var comp))
                {
                    compStatus = comp.ComplianceStatus?.ToUpperInvariant() == "COMPLIANT" ? "COMPLIANT" : "NOT COMPLIANT";
                    
                    if (compStatus == "COMPLIANT")
                    {
                        compReasons = $"All {comp.ValidCount ?? 5} documents verified & valid";
                    }
                    else
                    {
                        var parts = new List<string>();
                        if ((comp.ExpiredCount ?? 0) > 0) parts.Add($"{comp.ExpiredCount} expired");
                        if ((comp.RejectedCount ?? 0) > 0) parts.Add($"{comp.RejectedCount} rejected");
                        if ((comp.MissingCount ?? 0) > 0) parts.Add($"{comp.MissingCount} missing");
                        if ((comp.PendingVerificationCount ?? 0) > 0) parts.Add($"{comp.PendingVerificationCount} pending verification");
                        compReasons = parts.Any() ? string.Join(" · ", parts) : "Compliance incomplete";
                    }
                }

                list.Add(new SupplierDisplayItem
                {
                    Id = s.SupplierId,
                    Name = s.SupplierName,
                    ContactPerson = s.ContactName,
                    Email = s.Email,
                    Status = s.Status,
                    ComplianceStatus = compStatus,
                    ComplianceReasons = compReasons
                });
            }

            Suppliers = list;
        }

        public async Task<IActionResult> OnPostRegisterAsync([FromForm] string name, [FromForm] string contact, [FromForm] string email)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return RedirectToPage();
            }

            var org = await _context.Organizations.FirstOrDefaultAsync();
            int orgId = org?.OrganizationId ?? 1;

            var userDisplayName = HttpContext.Session.GetString("FullName") ?? "Administrator";
            var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.RoleId == 2);

            var supplier = new DoComply.Models.Supplier
            {
                OrganizationId = orgId,
                SupplierName = name.Trim(),
                ContactName = string.IsNullOrWhiteSpace(contact) ? "Operations Contact" : contact.Trim(),
                Email = string.IsNullOrWhiteSpace(email) ? "supplier@company.com" : email.Trim(),
                Status = "Active",
                CreatedByUserId = adminUser?.UserId,
                RegisteredAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            _context.Suppliers.Add(supplier);

            _context.AuditLogs.Add(new AuditLog
            {
                OrganizationId = orgId,
                UserDisplayName = userDisplayName,
                Action = $"Registered supplier ({supplier.SupplierName})",
                Module = "Supplier",
                OccurredAtUtc = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}
