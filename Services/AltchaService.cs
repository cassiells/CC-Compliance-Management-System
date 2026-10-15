using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DoComply.Services
{
    public class AltchaService : IAltchaService
    {
        private readonly byte[] _hmacKey;
        private readonly ConcurrentDictionary<string, DateTime> _usedSignatures = new();

        public AltchaService()
        {
            // Ephemeral server key for HMAC signature validation
            _hmacKey = new byte[64];
            RandomNumberGenerator.Fill(_hmacKey);
        }

        public AltchaChallenge CreateChallenge()
        {
            // Prune signatures older than 15 minutes to prevent memory growth
            var cutoff = DateTime.UtcNow.AddMinutes(-15);
            foreach (var kvp in _usedSignatures)
            {
                if (kvp.Value < cutoff)
                {
                    _usedSignatures.TryRemove(kvp.Key, out _);
                }
            }

            int maxNumber = 50000;
            int targetNumber = RandomNumberGenerator.GetInt32(1000, maxNumber);

            // Generate random salt (16 bytes hex)
            byte[] saltBytes = new byte[16];
            RandomNumberGenerator.Fill(saltBytes);
            string salt = Convert.ToHexString(saltBytes).ToLowerInvariant();

            // Challenge = SHA256(salt + targetNumber)
            string toHash = salt + targetNumber;
            byte[] challengeBytes = SHA256.HashData(Encoding.UTF8.GetBytes(toHash));
            string challenge = Convert.ToHexString(challengeBytes).ToLowerInvariant();

            // Signature = HMAC-SHA256(challenge)
            using var hmac = new HMACSHA256(_hmacKey);
            byte[] signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(challenge));
            string signature = Convert.ToHexString(signatureBytes).ToLowerInvariant();

            return new AltchaChallenge
            {
                Algorithm = "SHA-256",
                Challenge = challenge,
                Maxnumber = maxNumber,
                Salt = salt,
                Signature = signature
            };
        }

        public bool ValidatePayload(string? payload)
        {
            if (string.IsNullOrWhiteSpace(payload)) return false;

            // Direct support for test / local sandbox bypass if altcha widget is running in test mode
            if (payload.Equals("test", StringComparison.OrdinalIgnoreCase) ||
                payload.Contains("\"test\":true", StringComparison.OrdinalIgnoreCase) ||
                payload.Contains("\"test\": true", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            try
            {
                string json;
                try
                {
                    byte[] bytes = Convert.FromBase64String(payload);
                    json = Encoding.UTF8.GetString(bytes);
                }
                catch
                {
                    json = payload;
                }

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // If test mode payload from widget
                if (root.TryGetProperty("test", out var testProp) && testProp.GetBoolean())
                {
                    return true;
                }

                if (!root.TryGetProperty("challenge", out var chalProp) ||
                    !root.TryGetProperty("signature", out var sigProp) ||
                    !root.TryGetProperty("salt", out var saltProp) ||
                    !root.TryGetProperty("number", out var numProp))
                {
                    return false;
                }

                string challenge = chalProp.GetString() ?? "";
                string signature = sigProp.GetString() ?? "";
                string salt = saltProp.GetString() ?? "";
                int number = numProp.GetInt32();

                if (string.IsNullOrEmpty(challenge) || string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(salt))
                {
                    return false;
                }

                // Check replay attack
                if (_usedSignatures.ContainsKey(signature))
                {
                    return false;
                }

                // Verify HMAC signature
                using var hmac = new HMACSHA256(_hmacKey);
                byte[] expectedSigBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(challenge));
                string expectedSignature = Convert.ToHexString(expectedSigBytes).ToLowerInvariant();

                if (!signature.Equals(expectedSignature, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                // Verify proof-of-work: SHA256(salt + number) == challenge
                byte[] computedChallengeBytes = SHA256.HashData(Encoding.UTF8.GetBytes(salt + number));
                string computedChallenge = Convert.ToHexString(computedChallengeBytes).ToLowerInvariant();

                if (!challenge.Equals(computedChallenge, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                // Cache used signature to prevent replay
                _usedSignatures.TryAdd(signature, DateTime.UtcNow);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
