using System;
using System.Collections.Concurrent;

namespace DoComply.Services
{
    public class UserLockoutRecord
    {
        public int FailedAttempts { get; set; } = 0;
        public int LockoutTier { get; set; } = 0; // 1 = 1 minute, 2 = 2 minutes, 3 = 3 minutes, etc.
        public DateTime? LockoutUntilUtc { get; set; }
    }

    public class LoginLockoutService : ILoginLockoutService
    {
        private readonly ConcurrentDictionary<string, UserLockoutRecord> _records = new(StringComparer.OrdinalIgnoreCase);

        public LockoutStatusResult GetLockoutStatus(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return new LockoutStatusResult { IsLockedOut = false, RemainingSeconds = 0, FailedAttempts = 0 };
            }

            var clean = username.Trim().ToLowerInvariant();
            if (!_records.TryGetValue(clean, out var record))
            {
                return new LockoutStatusResult { IsLockedOut = false, RemainingSeconds = 0, FailedAttempts = 0, NextLockoutMinutes = 1 };
            }

            if (record.LockoutUntilUtc.HasValue && record.LockoutUntilUtc.Value > DateTime.UtcNow)
            {
                var remaining = (int)Math.Ceiling((record.LockoutUntilUtc.Value - DateTime.UtcNow).TotalSeconds);
                return new LockoutStatusResult
                {
                    IsLockedOut = true,
                    RemainingSeconds = Math.Max(1, remaining),
                    CurrentLockoutMinutes = record.LockoutTier,
                    FailedAttempts = record.FailedAttempts,
                    NextLockoutMinutes = record.LockoutTier + 1,
                    Message = $"Account locked due to multiple failed login attempts. Please wait {remaining} seconds before trying again."
                };
            }

            // Lockout period has elapsed
            if (record.LockoutUntilUtc.HasValue && record.LockoutUntilUtc.Value <= DateTime.UtcNow)
            {
                record.LockoutUntilUtc = null;
            }

            return new LockoutStatusResult
            {
                IsLockedOut = false,
                RemainingSeconds = 0,
                CurrentLockoutMinutes = record.LockoutTier,
                FailedAttempts = record.FailedAttempts,
                NextLockoutMinutes = record.LockoutTier + 1
            };
        }

        public LockoutStatusResult RecordFailedAttempt(string username)
        {
            var clean = (username ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(clean))
            {
                clean = "unknown";
            }

            var record = _records.GetOrAdd(clean, _ => new UserLockoutRecord());

            // If user is already locked out, keep them locked out and return remaining seconds
            if (record.LockoutUntilUtc.HasValue && record.LockoutUntilUtc.Value > DateTime.UtcNow)
            {
                var remaining = (int)Math.Ceiling((record.LockoutUntilUtc.Value - DateTime.UtcNow).TotalSeconds);
                return new LockoutStatusResult
                {
                    IsLockedOut = true,
                    RemainingSeconds = Math.Max(1, remaining),
                    CurrentLockoutMinutes = record.LockoutTier,
                    FailedAttempts = record.FailedAttempts,
                    NextLockoutMinutes = record.LockoutTier + 1,
                    Message = $"Account currently locked. Please wait {remaining} seconds before trying again."
                };
            }

            record.FailedAttempts++;

            // Requirement: 5 failed attempts triggers a lockout.
            // 1st lockout: 1 minute (60s).
            // If user fails 5 more times after that: increases by 1 minute (2 minutes, then 3 minutes, etc.).
            if (record.FailedAttempts >= 5)
            {
                record.LockoutTier++; // Increases: 1, 2, 3...
                int lockoutMinutes = record.LockoutTier;
                record.LockoutUntilUtc = DateTime.UtcNow.AddMinutes(lockoutMinutes);
                record.FailedAttempts = 0; // Reset consecutive failures so next tier requires 5 new failures

                int seconds = lockoutMinutes * 60;
                return new LockoutStatusResult
                {
                    IsLockedOut = true,
                    RemainingSeconds = seconds,
                    CurrentLockoutMinutes = lockoutMinutes,
                    FailedAttempts = 0,
                    NextLockoutMinutes = record.LockoutTier + 1,
                    Message = $"Too many failed login attempts (5/5). Account locked for {lockoutMinutes} minute(s). Please wait for the countdown to complete."
                };
            }

            int attemptsLeft = 5 - record.FailedAttempts;
            return new LockoutStatusResult
            {
                IsLockedOut = false,
                RemainingSeconds = 0,
                CurrentLockoutMinutes = record.LockoutTier,
                FailedAttempts = record.FailedAttempts,
                NextLockoutMinutes = record.LockoutTier + 1,
                Message = $"Invalid username or password. {attemptsLeft} attempt(s) remaining before account lockout."
            };
        }

        public void ResetLockout(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return;
            var clean = username.Trim().ToLowerInvariant();
            _records.TryRemove(clean, out _);
        }
    }
}
