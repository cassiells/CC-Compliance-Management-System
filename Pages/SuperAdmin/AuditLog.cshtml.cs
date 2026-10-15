using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DoComply.Models;

namespace DoComply.Pages.SuperAdmin
{
    public class AuditLogModel : PageModel
    {
        private readonly DoComplyContext _context;

        public AuditLogModel(DoComplyContext context)
        {
            _context = context;
        }

        public List<AuditLogItem> Logs { get; set; } = new List<AuditLogItem>();

        public async Task OnGetAsync()
        {
            var dbLogs = await _context.AuditLogs
                .OrderByDescending(a => a.OccurredAtUtc)
                .Take(100)
                .ToListAsync();

            if (dbLogs.Any())
            {
                Logs = dbLogs.Select(a => new AuditLogItem
                {
                    User = a.UserDisplayName,
                    Action = a.Action,
                    Module = a.Module,
                    Date = a.OccurredAtUtc.ToLocalTime().ToString("MMM dd, yyyy hh:mm tt")
                }).ToList();
            }
            else
            {
                Logs = SampleDataFixture.GetSampleAuditLogs();
            }
        }
    }
}
