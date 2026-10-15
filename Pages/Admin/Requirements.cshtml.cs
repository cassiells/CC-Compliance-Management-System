using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DoComply.Models;

namespace DoComply.Pages.Admin
{
    public class RequirementsModel : PageModel
    {
        private readonly DoComplyContext _context;

        public RequirementsModel(DoComplyContext context)
        {
            _context = context;
        }

        public List<Requirement> RequirementsList { get; set; } = new List<Requirement>();

        public async Task OnGetAsync()
        {
            RequirementsList = await _context.Requirements
                .Where(r => r.IsActive)
                .OrderBy(r => r.RequirementName)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync([FromForm] string name, [FromForm] string appliesTo, [FromForm] string expiry)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return RedirectToPage();
            }

            var org = await _context.Organizations.FirstOrDefaultAsync();
            int orgId = org?.OrganizationId ?? 1;

            bool expiryRequired = expiry?.Equals("Yes", StringComparison.OrdinalIgnoreCase) == true;

            var req = new Requirement
            {
                OrganizationId = orgId,
                RequirementName = name.Trim(),
                AppliesTo = "Supplier",
                ExpiryRequired = expiryRequired,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            _context.Requirements.Add(req);

            var userDisplayName = HttpContext.Session.GetString("FullName") ?? "Administrator";
            _context.AuditLogs.Add(new AuditLog
            {
                OrganizationId = orgId,
                UserDisplayName = userDisplayName,
                Action = $"Added compliance requirement ({req.RequirementName})",
                Module = "Compliance",
                OccurredAtUtc = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return new JsonResult(new
                {
                    success = true,
                    id = req.RequirementId,
                    name = req.RequirementName,
                    appliesTo = req.AppliesTo,
                    expiry = req.ExpiryRequired ? "Yes" : "No"
                });
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateAsync([FromForm] int id, [FromForm] string name, [FromForm] string appliesTo, [FromForm] string expiry)
        {
            if (id <= 0 || string.IsNullOrWhiteSpace(name))
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return new JsonResult(new { success = false, message = "Requirement Name is required." }) { StatusCode = 400 };
                }
                return RedirectToPage();
            }

            var req = await _context.Requirements.FirstOrDefaultAsync(r => r.RequirementId == id);
            if (req == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return new JsonResult(new { success = false, message = "Requirement not found." }) { StatusCode = 404 };
                }
                return RedirectToPage();
            }

            var trimmedName = name.Trim();
            // Check unique constraint within organization
            var duplicate = await _context.Requirements
                .AnyAsync(r => r.OrganizationId == req.OrganizationId && r.RequirementId != id && r.RequirementName.ToLower() == trimmedName.ToLower());
            if (duplicate)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return new JsonResult(new { success = false, message = $"A requirement named '{trimmedName}' already exists." }) { StatusCode = 400 };
                }
                return RedirectToPage();
            }

            bool expiryRequired = expiry?.Equals("Yes", StringComparison.OrdinalIgnoreCase) == true;

            req.RequirementName = trimmedName;
            if (!string.IsNullOrWhiteSpace(appliesTo))
            {
                req.AppliesTo = appliesTo.Trim();
            }
            req.ExpiryRequired = expiryRequired;
            req.UpdatedAtUtc = DateTime.UtcNow;

            var org = await _context.Organizations.FirstOrDefaultAsync();
            int orgId = org?.OrganizationId ?? req.OrganizationId;
            var userDisplayName = HttpContext.Session.GetString("FullName") ?? "Administrator";

            _context.AuditLogs.Add(new AuditLog
            {
                OrganizationId = orgId,
                UserDisplayName = userDisplayName,
                Action = $"Updated compliance requirement ({req.RequirementName})",
                Module = "Compliance",
                OccurredAtUtc = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return new JsonResult(new
                {
                    success = true,
                    id = req.RequirementId,
                    name = req.RequirementName,
                    appliesTo = req.AppliesTo,
                    expiry = req.ExpiryRequired ? "Yes" : "No"
                });
            }

            return RedirectToPage();
        }
    }
}
