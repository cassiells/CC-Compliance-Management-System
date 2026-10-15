using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DoComply.Models;
using BCrypt.Net;

namespace DoComply.Pages.SuperAdmin
{
    [IgnoreAntiforgeryToken]
    public class UsersModel : PageModel
    {
        private readonly DoComplyContext _context;

        public UsersModel(DoComplyContext context)
        {
            _context = context;
        }

        public List<User> UsersList { get; set; } = new();

        public async Task OnGetAsync()
        {
            UsersList = await _context.Users
                .Include(u => u.Role)
                .OrderByDescending(u => u.CreatedAtUtc)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostCreateUserAsync([FromBody] CreateUserRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.FullName) || string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            {
                return BadRequest(new { success = false, message = "Missing required fields." });
            }

            var cleanUsername = req.Username.Trim().ToLower();
            if (!cleanUsername.Contains("@") || !cleanUsername.Contains("."))
            {
                return BadRequest(new { success = false, message = "Username must be a valid email address (e.g. user@company.com)." });
            }

            if (await _context.Users.AnyAsync(u => u.Username.ToLower() == cleanUsername))
            {
                return BadRequest(new { success = false, message = "A user with this username already exists." });
            }

            byte roleId = 2; // Default Administrator
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName.ToLower() == (req.Role ?? "").Trim().ToLower());
            if (role != null)
            {
                roleId = role.RoleId;
            }
            else
            {
                if (req.Role?.Contains("Super", StringComparison.OrdinalIgnoreCase) == true) roleId = 1;
                else if (req.Role?.Contains("Staff", StringComparison.OrdinalIgnoreCase) == true || req.Role?.Contains("HR", StringComparison.OrdinalIgnoreCase) == true) roleId = 3;
                else if (req.Role?.Contains("Supplier", StringComparison.OrdinalIgnoreCase) == true) roleId = 4;
            }

            // Hash password with BCrypt
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(req.Password);

            var newUser = new User
            {
                OrganizationId = 1,
                RoleId = roleId,
                FullName = req.FullName.Trim(),
                Username = cleanUsername,
                PasswordHash = passwordHash,
                Status = "Active",
                MustChangePassword = false,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            _context.Users.Add(newUser);

            _context.AuditLogs.Add(new AuditLog
            {
                OrganizationId = 1,
                UserDisplayName = "Super Admin",
                Action = $"Created account ({newUser.Username})",
                Module = "User Management",
                OccurredAtUtc = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            var roleName = (await _context.Roles.FindAsync(roleId))?.RoleName ?? "Administrator";

            return new JsonResult(new
            {
                success = true,
                user = new
                {
                    id = newUser.UserId,
                    name = newUser.FullName,
                    role = roleName,
                    username = newUser.Username,
                    status = newUser.Status
                }
            });
        }

        public async Task<IActionResult> OnPostChangePasswordAsync([FromBody] ChangePasswordRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.NewPassword))
            {
                return BadRequest(new { success = false, message = "Username and new password are required." });
            }

            var cleanUsername = req.Username.Trim().ToLower();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == cleanUsername);
            if (user == null)
            {
                return NotFound(new { success = false, message = "User not found." });
            }

            // Hash new password with BCrypt
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
            user.PasswordChangedAtUtc = DateTime.UtcNow;
            user.UpdatedAtUtc = DateTime.UtcNow;

            _context.AuditLogs.Add(new AuditLog
            {
                OrganizationId = user.OrganizationId,
                UserId = user.UserId,
                UserDisplayName = "Super Admin",
                Action = $"Changed password for user ({user.Username})",
                Module = "User Management",
                OccurredAtUtc = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }
    }

    public class CreateUserRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class ChangePasswordRequest
    {
        public string Username { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
