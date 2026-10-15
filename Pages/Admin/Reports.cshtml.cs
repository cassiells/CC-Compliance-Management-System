using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DoComply.Models;

using DoComply.Services;

namespace DoComply.Pages.Admin
{
    public class ReportsModel : PageModel
    {
        private readonly DoComplyContext _context;
        private readonly IReportPdfService _pdfService;

        public ReportsModel(DoComplyContext context, IReportPdfService pdfService)
        {
            _context = context;
            _pdfService = pdfService;
        }

        public List<VwComplianceReport> ComplianceReports { get; set; } = new();
        public List<VwExpiredDocumentsReport> ExpiredReports { get; set; } = new();
        public List<VwExpiringDocumentsReport> ExpiringReports { get; set; } = new();

        // Server-rendered KPI aggregates
        public int TotalSuppliers { get; set; }
        public int CompliantCount { get; set; }
        public int NotCompliantCount { get; set; }
        public int ComplianceRate { get; set; }

        public int TotalExpired { get; set; }
        public int ExpiredSuppliersCount { get; set; }

        public int TotalExpiring { get; set; }
        public int CriticalExpiringCount { get; set; }
        public int UpcomingExpiringCount { get; set; }

        public async Task OnGetAsync()
        {
            ComplianceReports = await _context.VwComplianceReports
                .OrderBy(r => r.SupplierName)
                .ToListAsync();

            TotalSuppliers = ComplianceReports.Count;
            CompliantCount = ComplianceReports.Count(r => r.ComplianceStatus == "Compliant");
            NotCompliantCount = ComplianceReports.Count(r => r.ComplianceStatus != "Compliant");
            ComplianceRate = TotalSuppliers > 0 ? (int)Math.Round((double)CompliantCount / TotalSuppliers * 100) : 0;

            ExpiredReports = await _context.VwExpiredDocumentsReports
                .OrderBy(r => r.SupplierName)
                .ToListAsync();

            TotalExpired = ExpiredReports.Count;
            ExpiredSuppliersCount = ExpiredReports.Select(r => r.SupplierName).Distinct().Count();

            ExpiringReports = await _context.VwExpiringDocumentsReports
                .OrderBy(r => r.ExpiryDate)
                .ToListAsync();

            TotalExpiring = ExpiringReports.Count;
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            CriticalExpiringCount = ExpiringReports.Count(r => r.ExpiryDate.HasValue && (r.ExpiryDate.Value.DayNumber - today.DayNumber) <= 15);
            UpcomingExpiringCount = TotalExpiring - CriticalExpiringCount;
        }

        public async Task<IActionResult> OnGetExportPdfAsync([FromQuery] string report, [FromQuery] string? search, [FromQuery] string? status, [FromQuery] string? range)
        {
            var userDisplayName = HttpContext.Session.GetString("FullName") ?? "Administrator";
            var reportType = (report ?? "compliance").Trim().ToLowerInvariant();

            if (reportType == "expired")
            {
                var data = await _context.VwExpiredDocumentsReports.OrderBy(r => r.SupplierName).ToListAsync();
                var bytes = _pdfService.GenerateExpiredDocumentsReportPdf(data, search, userDisplayName);
                var filename = $"CC_Admin_Expired_Documents_Report_{DateTime.UtcNow:yyyyMMdd_HHmm}.pdf";
                return File(bytes, "application/pdf", filename);
            }
            else if (reportType == "expiring")
            {
                var data = await _context.VwExpiringDocumentsReports.OrderBy(r => r.ExpiryDate).ToListAsync();
                var bytes = _pdfService.GenerateExpiringDocumentsReportPdf(data, search, range, userDisplayName);
                var filename = $"CC_Admin_Expiring_Documents_Report_{DateTime.UtcNow:yyyyMMdd_HHmm}.pdf";
                return File(bytes, "application/pdf", filename);
            }
            else
            {
                var data = await _context.VwComplianceReports.OrderBy(r => r.SupplierName).ToListAsync();
                var bytes = _pdfService.GenerateComplianceReportPdf(data, search, status, userDisplayName);
                var filename = $"CC_Admin_Compliance_Standing_Report_{DateTime.UtcNow:yyyyMMdd_HHmm}.pdf";
                return File(bytes, "application/pdf", filename);
            }
        }
    }
}
