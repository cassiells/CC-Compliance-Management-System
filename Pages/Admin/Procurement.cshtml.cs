using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DoComply.Models;

namespace DoComply.Pages.Admin
{
    public class ProcurementModel : PageModel
    {
        private readonly DoComplyContext _context;

        public ProcurementModel(DoComplyContext context)
        {
            _context = context;
        }

        public List<ProcurementRequestItem> Requests { get; set; } = new List<ProcurementRequestItem>();

        public async Task OnGetAsync()
        {
            var dbReqs = await _context.ProcurementRequests
                .Include(p => p.Supplier)
                .Include(p => p.RequestedByUser)
                .OrderByDescending(p => p.CreatedAtUtc)
                .ToListAsync();

            if (dbReqs.Any())
            {
                var complianceMap = await _context.VwSupplierCompliances
                    .ToDictionaryAsync(c => c.SupplierId, c => c.ComplianceStatus);

                Requests = dbReqs.Select(r =>
                {
                    bool isCompliant = true;
                    if (complianceMap.TryGetValue(r.SupplierId, out var status))
                    {
                        isCompliant = status?.Equals("Compliant", System.StringComparison.OrdinalIgnoreCase) == true;
                    }

                    return new ProcurementRequestItem
                    {
                        Id = (int)r.ProcurementRequestId,
                        RequestName = r.RequestName,
                        SupplierName = r.Supplier.SupplierName,
                        RequestedBy = $"{r.RequestedByUser.FullName} (Staff)",
                        Purpose = r.Purpose,
                        DateSubmitted = r.CreatedAtUtc.ToLocalTime().ToString("MMM dd, yyyy"),
                        Status = r.Status,
                        IsSupplierCompliant = isCompliant
                    };
                }).ToList();
            }
            else
            {
                Requests = SampleDataFixture.GetSampleProcurementRequests();
            }
        }
    }
}
