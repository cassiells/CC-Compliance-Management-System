using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DoComply.Pages
{
    public class IndexModel : PageModel
    {
        public IActionResult OnGet()
        {
            var sessionUserId = HttpContext.Session.GetString("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (!string.IsNullOrEmpty(sessionUserId) && roleId.HasValue)
            {
                return roleId.Value switch
                {
                    1 => RedirectToPage("/SuperAdmin/Overview"),
                    3 => RedirectToPage("/Staff/Suppliers"),
                    4 => RedirectToPage("/Supplier/MyCompliance"),
                    _ => RedirectToPage("/Admin/Dashboard")
                };
            }

            return RedirectToPage("/Account/Login");
        }
    }
}
