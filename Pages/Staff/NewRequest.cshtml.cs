using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DoComply.Models;

namespace DoComply.Pages.Staff
{
    public class NewRequestModel : PageModel
    {
        private readonly DoComplyContext _context;

        public NewRequestModel(DoComplyContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string Supplier { get; set; } = "ABC Office Supplies";

        [BindProperty]
        public string RequestName { get; set; } = string.Empty;

        [BindProperty]
        public string Purpose { get; set; } = string.Empty;

        public List<string> SupplierOptions { get; set; } = new List<string>();

        public async Task OnGetAsync()
        {
            var sups = await _context.Suppliers
                .OrderBy(s => s.SupplierName)
                .Select(s => s.SupplierName)
                .ToListAsync();

            if (sups.Any())
            {
                SupplierOptions = sups;
            }
            else
            {
                SupplierOptions = new List<string>
                {
                    "ABC Office Supplies",
                    "XYZ Training Services",
                    "DEF Equipment Corp.",
                    "Global Logistics Inc."
                };
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(RequestName) || string.IsNullOrWhiteSpace(Supplier))
            {
                return RedirectToPage("/Staff/MyRequests");
            }

            var sup = await _context.Suppliers.FirstOrDefaultAsync(s => s.SupplierName == Supplier);
            if (sup == null)
            {
                return RedirectToPage("/Staff/MyRequests");
            }

            var staffUser = await _context.Users.FirstOrDefaultAsync(u => u.RoleId == 3);
            long requesterId = staffUser?.UserId ?? 1;

            var userDisplayName = HttpContext.Session.GetString("FullName") ?? "Leah Ramos";

            var req = new ProcurementRequest
            {
                OrganizationId = sup.OrganizationId,
                SupplierId = sup.SupplierId,
                RequestedByUserId = requesterId,
                RequestName = RequestName.Trim(),
                Purpose = string.IsNullOrWhiteSpace(Purpose) ? "Procurement for department operational needs." : Purpose.Trim(),
                Status = "Pending Approval",
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.ProcurementRequests.Add(req);

            _context.AuditLogs.Add(new AuditLog
            {
                OrganizationId = sup.OrganizationId,
                UserDisplayName = $"{userDisplayName} (Staff)",
                Action = $"Created procurement request ({req.RequestName}) for {sup.SupplierName}",
                Module = "Procurement",
                OccurredAtUtc = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return RedirectToPage("/Staff/MyRequests");
        }
    }
}
