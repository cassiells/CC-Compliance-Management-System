using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DoComply.Models;

namespace DoComply.Pages.Supplier
{
    public class NotificationsModel : PageModel
    {
        private readonly DoComplyContext _context;

        public NotificationsModel(DoComplyContext context)
        {
            _context = context;
        }

        public string CompanyName { get; set; } = "ABC Office Supplies";
        public List<(long Id, string Type, string Title, string Message, string Time, bool Unread)> NotificationsList { get; set; } = new();
        public int UnreadCount { get; set; } = 0;

        public async Task OnGetAsync()
        {
            var sessionName = HttpContext.Session.GetString("FullName");
            var sessionUserId = HttpContext.Session.GetString("UserId");

            long currentUserId = 0;
            if (!string.IsNullOrEmpty(sessionUserId) && long.TryParse(sessionUserId, out var uid))
            {
                currentUserId = uid;
            }
            else
            {
                var supUser = await _context.Users.FirstOrDefaultAsync(u => u.RoleId == 4);
                currentUserId = supUser?.UserId ?? 4;
            }

            var sup = await _context.Suppliers.FirstOrDefaultAsync(s => 
                (sessionName != null && s.SupplierName.Contains(sessionName)) || 
                s.SupplierName == "ABC Office Supplies" ||
                s.CreatedByUserId == currentUserId);

            if (sup != null)
            {
                CompanyName = sup.SupplierName;
            }

            long? supplierId = sup?.SupplierId;

            var dbNotifs = await _context.Notifications
                .Where(n => n.UserId == currentUserId)
                .OrderByDescending(n => n.CreatedAtUtc)
                .ToListAsync();

            NotificationsList = dbNotifs.Select(n =>
            {
                string title = n.NotificationType.ToLower() switch
                {
                    "warning" or "expiring" => "Expiration Reminder",
                    "danger" or "expired" => "Compliance Alert",
                    "rejected" => "Document Rejected",
                    "success" or "approved" => "Verification Decision",
                    _ => "Compliance Notice"
                };

                string timeStr = (DateTime.UtcNow - n.CreatedAtUtc).TotalHours < 24
                    ? n.CreatedAtUtc.ToLocalTime().ToString("hh:mm tt")
                    : n.CreatedAtUtc.ToLocalTime().ToString("MMM dd, yyyy");

                return (n.NotificationId, n.NotificationType, title, n.Message, timeStr, !n.IsRead);
            }).ToList();

            UnreadCount = NotificationsList.Count(n => n.Unread);
        }

        public async Task<IActionResult> OnPostMarkAllReadAsync()
        {
            var sessionUserId = HttpContext.Session.GetString("UserId");
            long currentUserId = 0;
            if (!string.IsNullOrEmpty(sessionUserId) && long.TryParse(sessionUserId, out var uid))
            {
                currentUserId = uid;
            }
            else
            {
                var supUser = await _context.Users.FirstOrDefaultAsync(u => u.RoleId == 4);
                currentUserId = supUser?.UserId ?? 4;
            }

            var unread = await _context.Notifications
                .Where(n => n.UserId == currentUserId && !n.IsRead)
                .ToListAsync();

            foreach (var n in unread)
            {
                n.IsRead = true;
                n.ReadAtUtc = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostMarkReadAsync(long id)
        {
            var notif = await _context.Notifications.FirstOrDefaultAsync(n => n.NotificationId == id);
            if (notif != null)
            {
                notif.IsRead = true;
                notif.ReadAtUtc = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }
    }
}
