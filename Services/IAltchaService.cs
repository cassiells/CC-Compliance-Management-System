using System.Text.Json.Serialization;

namespace DoComply.Services
{
    public class AltchaChallenge
    {
        [JsonPropertyName("algorithm")]
        public string Algorithm { get; set; } = "SHA-256";

        [JsonPropertyName("challenge")]
        public string Challenge { get; set; } = string.Empty;

        [JsonPropertyName("maxnumber")]
        public int Maxnumber { get; set; } = 50000;

        [JsonPropertyName("salt")]
        public string Salt { get; set; } = string.Empty;

        [JsonPropertyName("signature")]
        public string Signature { get; set; } = string.Empty;
    }

    public interface IAltchaService
    {
        AltchaChallenge CreateChallenge();
        bool ValidatePayload(string? payload);
    }
}
