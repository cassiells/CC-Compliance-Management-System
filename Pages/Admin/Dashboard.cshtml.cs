using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DoComply.Models;

namespace DoComply.Pages.Admin
{
    public class DashboardModel : PageModel
    {
        private readonly DoComplyContext _context;

        public DashboardModel(DoComplyContext context)
        {
            _context = context;
        }

        public string AdminName { get; set; } = "Juan";
        public int CompliantCount { get; set; } = 0;
        public int NotCompliantCount { get; set; } = 0;
        public int PendingVerificationCount { get; set; } = 0;
        public int ExpiringSoonCount { get; set; } = 0;
        public int ExpiredCount { get; set; } = 0;
        public List<AuditLog> RecentActivities { get; set; } = new List<AuditLog>();
        public List<string> ExpiredSupplierNames { get; set; } = new List<string>();

        public async Task OnGetAsync()
        {
            var sessionName = HttpContext.Session.GetString("FullName");
            if (!string.IsNullOrEmpty(sessionName))
            {
                AdminName = sessionName.Split(' ')[0];
            }

            CompliantCount = await _context.VwSupplierCompliances.CountAsync(c => c.ComplianceStatus == "Compliant");
            NotCompliantCount = await _context.VwSupplierCompliances.CountAsync(c => c.ComplianceStatus != "Compliant");
            PendingVerificationCount = await _context.DocumentSubmissions.CountAsync(d => d.Status == "Pending Verification");
            ExpiringSoonCount = await _context.DocumentSubmissions.CountAsync(d => d.Status == "Expiring Soon");
            ExpiredCount = await _context.DocumentSubmissions.CountAsync(d => d.Status == "Expired");

            RecentActivities = await _context.AuditLogs
                .OrderByDescending(a => a.OccurredAtUtc)
                .Take(5)
                .ToListAsync();

            ExpiredSupplierNames = await _context.VwExpiredDocumentsReports
                .Select(e => e.SupplierName)
                .Distinct()
                .Take(3)
                .ToListAsync();
        }
    }
}
