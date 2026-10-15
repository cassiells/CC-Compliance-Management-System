using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DoComply.Models;

namespace DoComply.Pages.Staff
{
    public class SuppliersModel : PageModel
    {
        private readonly DoComplyContext _context;

        public SuppliersModel(DoComplyContext context)
        {
            _context = context;
        }

        public List<SupplierDataItem> Suppliers { get; set; } = new List<SupplierDataItem>();

        public async Task OnGetAsync()
        {
            var dbSuppliers = await _context.Suppliers
                .OrderBy(s => s.SupplierName)
                .ToListAsync();

            if (dbSuppliers.Any())
            {
                var complianceMap = await _context.VwSupplierCompliances
                    .ToDictionaryAsync(c => c.SupplierId, c => c);

                Suppliers = dbSuppliers.Select(s =>
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

                    return new SupplierDataItem
                    {
                        Name = s.SupplierName,
                        ContactPerson = s.ContactName,
                        Email = s.Email,
                        Status = s.Status,
                        ComplianceStatus = compStatus,
                        ComplianceReasons = compReasons
                    };
                }).ToList();
            }
            else
            {
                Suppliers = SampleDataFixture.GetSampleSuppliers();
            }
        }
    }
}
