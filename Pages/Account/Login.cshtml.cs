using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DoComply.Models;
using DoComply.Services;
using BCrypt.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace DoComply.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly DoComplyContext _context;
        private readonly IAltchaService _altchaService;
        private readonly ILoginLockoutService _lockoutService;

        public LoginModel(
            DoComplyContext context,
            IAltchaService altchaService,
            ILoginLockoutService lockoutService)
        {
            _context = context;
            _altchaService = altchaService;
            _lockoutService = lockoutService;
        }

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        [BindProperty]
        public string Username { get; set; } = "juan@cccorp.com";

        [BindProperty]
        public string Password { get; set; } = "AdminPass123!";

        [BindProperty]
        public string TargetRole { get; set; } = "Administrator";

        [BindProperty]
        public string? Altcha { get; set; }

        public string? ErrorMessage { get; set; }
        public bool IsLockedOut { get; set; }
        public int LockoutRemainingSeconds { get; set; }
        public int CurrentLockoutMinutes { get; set; }
        public int FailedAttemptsCount { get; set; }

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

            if (!string.IsNullOrWhiteSpace(Username))
            {
                var status = _lockoutService.GetLockoutStatus(Username);
                IsLockedOut = status.IsLockedOut;
                LockoutRemainingSeconds = status.RemainingSeconds;
                CurrentLockoutMinutes = status.CurrentLockoutMinutes;
                FailedAttemptsCount = status.FailedAttempts;
            }

            return Page();
        }

        public IActionResult OnGetAltchaChallenge()
        {
            var challenge = _altchaService.CreateChallenge();
            return new JsonResult(challenge);
        }

        public IActionResult OnGetLockoutStatus([FromQuery] string? username)
        {
            var status = _lockoutService.GetLockoutStatus(username ?? "");
            return new JsonResult(status);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var cleanUsername = (Username ?? "").Trim().ToLower();

            // 1. Enforce incremental login lockout check
            var lockoutCheck = _lockoutService.GetLockoutStatus(cleanUsername);
            if (lockoutCheck.IsLockedOut)
            {
                IsLockedOut = true;
                LockoutRemainingSeconds = lockoutCheck.RemainingSeconds;
                CurrentLockoutMinutes = lockoutCheck.CurrentLockoutMinutes;
                FailedAttemptsCount = lockoutCheck.FailedAttempts;
                ErrorMessage = lockoutCheck.Message ?? $"Account is locked. Please wait {lockoutCheck.RemainingSeconds}s.";
                return Page();
            }

            // 2. Enforce Altcha verification check
            if (string.IsNullOrWhiteSpace(Altcha) || !_altchaService.ValidatePayload(Altcha))
            {
                ErrorMessage = "Security verification required. Please click and complete the Altcha verification before signing in.";
                return Page();
            }

            // 3. Credentials presence check
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Please enter both username and password.";
                return Page();
            }

            // 4. Verify user in database
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username.ToLower() == cleanUsername);

            bool isPasswordValid = false;
            if (user != null)
            {
                try
                {
                    if (!string.IsNullOrEmpty(user.PasswordHash) && user.PasswordHash.StartsWith("$2"))
                    {
                        isPasswordValid = BCrypt.Net.BCrypt.Verify(Password, user.PasswordHash);
                    }
                }
                catch
                {
                    isPasswordValid = false;
                }
            }

            if (user == null || !isPasswordValid)
            {
                // Record failed login attempt (triggers lockout at 5, increases by 1 min on subsequent 5s)
                var failResult = _lockoutService.RecordFailedAttempt(cleanUsername);
                IsLockedOut = failResult.IsLockedOut;
                LockoutRemainingSeconds = failResult.RemainingSeconds;
                CurrentLockoutMinutes = failResult.CurrentLockoutMinutes;
                FailedAttemptsCount = failResult.FailedAttempts;
                ErrorMessage = failResult.Message ?? "Invalid username or password.";
                return Page();
            }

            // Reset lockout status on successful login
            _lockoutService.ResetLockout(cleanUsername);

            // Save login timestamp & audit log to database
            user.LastLoginAtUtc = DateTime.UtcNow;
            _context.AuditLogs.Add(new AuditLog
            {
                OrganizationId = user.OrganizationId,
                UserId = user.UserId,
                UserDisplayName = user.FullName,
                Action = "User logged in",
                Module = "User Management",
                OccurredAtUtc = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            // Set Cookie Authentication Claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("FullName", user.FullName),
                new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "Administrator"),
                new Claim("RoleId", user.RoleId.ToString())
            };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(4)
            };
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

            // Set session data
            HttpContext.Session.SetString("UserId", user.UserId.ToString());
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("FullName", user.FullName);
            HttpContext.Session.SetString("RoleName", user.Role?.RoleName ?? "Administrator");
            HttpContext.Session.SetInt32("RoleId", user.RoleId);

            // Check ReturnUrl authorization
            if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            {
                bool isAuthorizedForReturnUrl = user.RoleId switch
                {
                    1 => true,
                    2 => ReturnUrl.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase) || ReturnUrl.StartsWith("/Notifications", StringComparison.OrdinalIgnoreCase),
                    3 => ReturnUrl.StartsWith("/Staff", StringComparison.OrdinalIgnoreCase),
                    4 => ReturnUrl.StartsWith("/Supplier", StringComparison.OrdinalIgnoreCase),
                    _ => false
                };

                if (isAuthorizedForReturnUrl)
                {
                    return LocalRedirect(ReturnUrl);
                }
            }

            // Redirect based on role
            switch (user.RoleId)
            {
                case 1:
                    return RedirectToPage("/SuperAdmin/Overview");
                case 3:
                    return RedirectToPage("/Staff/Suppliers");
                case 4:
                    return RedirectToPage("/Supplier/MyCompliance");
                case 2:
                default:
                    return RedirectToPage("/Admin/Dashboard");
            }
        }
    }
}
