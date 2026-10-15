using System.Text.Json.Serialization;

namespace DoComply.Services
{
    public class LockoutStatusResult
    {
        [JsonPropertyName("isLockedOut")]
        public bool IsLockedOut { get; set; }

        [JsonPropertyName("remainingSeconds")]
        public int RemainingSeconds { get; set; }

        [JsonPropertyName("currentLockoutMinutes")]
        public int CurrentLockoutMinutes { get; set; }

        [JsonPropertyName("failedAttempts")]
        public int FailedAttempts { get; set; }

        [JsonPropertyName("maxAttemptsBeforeLockout")]
        public int MaxAttemptsBeforeLockout { get; set; } = 5;

        [JsonPropertyName("nextLockoutMinutes")]
        public int NextLockoutMinutes { get; set; } = 1;

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }

    public interface ILoginLockoutService
    {
        LockoutStatusResult GetLockoutStatus(string username);
        LockoutStatusResult RecordFailedAttempt(string username);
        void ResetLockout(string username);
    }
}
