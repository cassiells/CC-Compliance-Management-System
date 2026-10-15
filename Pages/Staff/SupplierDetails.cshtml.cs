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
    public class SupplierDetailsModel : PageModel
    {
        private readonly DoComplyContext _context;

        public SupplierDetailsModel(DoComplyContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public long? Id { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Name { get; set; } = "ABC Office Supplies";

        [BindProperty(SupportsGet = true)]
        public string Status { get; set; } = "NOT COMPLIANT";

        [BindProperty(SupportsGet = true)]
        public string Reasons { get; set; } = "1 missing · 1 pending verification";

        public List<VwSupplierChecklist> ChecklistItems { get; set; } = new List<VwSupplierChecklist>();

        public async Task OnGetAsync()
        {
            DoComply.Models.Supplier? supplier = null;
            if (Id.HasValue && Id.Value > 0)
            {
                supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.SupplierId == Id.Value);
            }
            if (supplier == null && !string.IsNullOrWhiteSpace(Name))
            {
                supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.SupplierName == Name);
            }

            if (supplier != null)
            {
                Id = supplier.SupplierId;
                Name = supplier.SupplierName;

                var compliance = await _context.VwSupplierCompliances.FirstOrDefaultAsync(c => c.SupplierId == supplier.SupplierId);
                if (compliance != null)
                {
                    Status = compliance.ComplianceStatus?.ToUpperInvariant() == "COMPLIANT" ? "COMPLIANT" : "NOT COMPLIANT";
                    if (Status == "COMPLIANT")
                    {
                        Reasons = $"All {compliance.ValidCount ?? 5} documents verified & valid";
                    }
                    else
                    {
                        var parts = new List<string>();
                        if ((compliance.ExpiredCount ?? 0) > 0) parts.Add($"{compliance.ExpiredCount} expired");
                        if ((compliance.RejectedCount ?? 0) > 0) parts.Add($"{compliance.RejectedCount} rejected");
                        if ((compliance.MissingCount ?? 0) > 0) parts.Add($"{compliance.MissingCount} missing");
                        if ((compliance.PendingVerificationCount ?? 0) > 0) parts.Add($"{compliance.PendingVerificationCount} pending verification");
                        Reasons = parts.Any() ? string.Join(" · ", parts) : "1 missing · 1 pending verification";
                    }
                }

                ChecklistItems = await _context.VwSupplierChecklists
                    .Where(c => c.SupplierId == supplier.SupplierId)
                    .OrderBy(c => c.RequirementName)
                    .ToListAsync();
            }
        }
    }
}
