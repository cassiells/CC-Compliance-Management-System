using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DoComply.Models;

namespace DoComply.Pages.SuperAdmin
{
    public class OverviewModel : PageModel
    {
        private readonly DoComplyContext _context;

        public OverviewModel(DoComplyContext context)
        {
            _context = context;
        }

        public int ActiveUsers { get; set; }
        public int Administrators { get; set; }
        public int SuppliersCount { get; set; }
        public int ActionsToday { get; set; }

        public int TotalUsers { get; set; }
        public int SupplierAccountsCount { get; set; }
        public int StaffAccountsCount { get; set; }
        public int AdminAccountsCount { get; set; }
        public int SuperAdminAccountsCount { get; set; }

        public int SupplierPercent => TotalUsers > 0 ? (int)Math.Round((double)SupplierAccountsCount / TotalUsers * 100) : 0;
        public int StaffPercent => TotalUsers > 0 ? (int)Math.Round((double)StaffAccountsCount / TotalUsers * 100) : 0;
        public int AdminPercent => TotalUsers > 0 ? (int)Math.Round((double)AdminAccountsCount / TotalUsers * 100) : 0;
        public int SuperAdminPercent => TotalUsers > 0 ? (int)Math.Round((double)SuperAdminAccountsCount / TotalUsers * 100) : 0;

        public async Task OnGetAsync()
        {
            ActiveUsers = await _context.Users.CountAsync(u => u.Status == "Active");
            Administrators = await _context.Users.CountAsync(u => u.RoleId == 2);
            SuppliersCount = await _context.Suppliers.CountAsync();

            var today = DateTime.UtcNow.Date;
            ActionsToday = await _context.AuditLogs.CountAsync(a => a.OccurredAtUtc >= today);
            if (ActionsToday == 0) ActionsToday = await _context.AuditLogs.CountAsync();

            TotalUsers = await _context.Users.CountAsync();
            SupplierAccountsCount = await _context.Users.CountAsync(u => u.RoleId == 4);
            StaffAccountsCount = await _context.Users.CountAsync(u => u.RoleId == 3);
            AdminAccountsCount = Administrators;
            SuperAdminAccountsCount = await _context.Users.CountAsync(u => u.RoleId == 1);
        }
    }
}
