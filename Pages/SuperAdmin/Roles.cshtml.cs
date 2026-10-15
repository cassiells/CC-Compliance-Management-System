using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using DoComply.Models;

namespace DoComply.Pages.SuperAdmin
{
    public class RolesModel : PageModel
    {
        private readonly DoComplyContext _context;

        public RolesModel(DoComplyContext context)
        {
            _context = context;
        }

        public List<Role> Roles { get; set; } = new List<Role>();

        public async Task OnGetAsync()
        {
            Roles = await _context.Roles
                .Include(r => r.Permissions)
                .Include(r => r.Users)
                .ToListAsync();
        }
    }
}
