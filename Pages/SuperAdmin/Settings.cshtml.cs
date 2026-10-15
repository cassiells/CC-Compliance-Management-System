using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DoComply.Models;

namespace DoComply.Pages.SuperAdmin
{
    public class SettingsModel : PageModel
    {
        private readonly DoComplyContext _context;

        public SettingsModel(DoComplyContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string OrgName { get; set; } = "CC Corporation";

        [BindProperty]
        public string SupportEmail { get; set; } = "compliance@cccorp.com";

        [BindProperty]
        public bool ExpiryReminders { get; set; } = true;

        [BindProperty]
        public bool ExpiredAlerts { get; set; } = true;

        [BindProperty]
        public bool VerificationOutcomes { get; set; } = true;

        public bool IsSaved { get; set; }

        public async Task OnGetAsync()
        {
            var org = await _context.Organizations.FirstOrDefaultAsync();
            if (org != null)
            {
                OrgName = org.OrganizationName;
                SupportEmail = org.SupportEmail ?? "compliance@cccorp.com";
            }

            var notif = await _context.NotificationSettings.FirstOrDefaultAsync();
            if (notif != null)
            {
                ExpiryReminders = notif.ExpirationRemindersEnabled;
                ExpiredAlerts = notif.ExpiredDocumentAlertsEnabled;
                VerificationOutcomes = notif.VerificationOutcomesEnabled;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var org = await _context.Organizations.FirstOrDefaultAsync();
            if (org == null)
            {
                org = new Organization
                {
                    OrganizationName = OrgName,
                    SupportEmail = SupportEmail,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                };
                _context.Organizations.Add(org);
            }
            else
            {
                org.OrganizationName = OrgName;
                org.SupportEmail = SupportEmail;
                org.UpdatedAtUtc = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            var notif = await _context.NotificationSettings.FirstOrDefaultAsync(n => n.OrganizationId == org.OrganizationId);
            if (notif == null)
            {
                notif = new NotificationSetting
                {
                    OrganizationId = org.OrganizationId,
                    ExpirationRemindersEnabled = ExpiryReminders,
                    ExpiredDocumentAlertsEnabled = ExpiredAlerts,
                    VerificationOutcomesEnabled = VerificationOutcomes,
                    UpdatedAtUtc = DateTime.UtcNow
                };
                _context.NotificationSettings.Add(notif);
            }
            else
            {
                notif.ExpirationRemindersEnabled = ExpiryReminders;
                notif.ExpiredDocumentAlertsEnabled = ExpiredAlerts;
                notif.VerificationOutcomesEnabled = VerificationOutcomes;
                notif.UpdatedAtUtc = DateTime.UtcNow;
            }

            _context.AuditLogs.Add(new AuditLog
            {
                OrganizationId = org.OrganizationId,
                UserDisplayName = "Super Admin",
                Action = "Updated system notification settings and organization profile",
                Module = "System Settings",
                OccurredAtUtc = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            IsSaved = true;

            return Page();
        }
    }
}
