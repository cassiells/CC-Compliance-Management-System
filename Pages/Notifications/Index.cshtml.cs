using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;

namespace DoComply.Pages.Notifications
{
    public class IndexModel : PageModel
    {
        public IActionResult OnGet()
        {
            var referer = Request.Headers["Referer"].ToString();
            var sessionRole = HttpContext.Session.GetString("RoleName");
            var roleQuery = Request.Query["role"].ToString();

            if (string.Equals(roleQuery, "Supplier", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(sessionRole, "Supplier", StringComparison.OrdinalIgnoreCase) ||
                referer.Contains("/Supplier", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToPage("/Supplier/Notifications");
            }

            if (string.Equals(roleQuery, "Staff", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(sessionRole, "Staff", StringComparison.OrdinalIgnoreCase) ||
                referer.Contains("/Staff", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToPage("/Staff/Notifications");
            }

            if (string.Equals(roleQuery, "SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(sessionRole, "Super Admin", StringComparison.OrdinalIgnoreCase) ||
                referer.Contains("/SuperAdmin", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToPage("/SuperAdmin/Notifications");
            }

            return RedirectToPage("/Admin/Notifications");
        }
    }
}
