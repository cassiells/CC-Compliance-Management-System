using System;
using System.Threading.Tasks;

namespace DoComply.Services
{
    public class BirTinVerificationResult
    {
        public bool IsValid { get; set; }
        public string FormattedTin { get; set; } = string.Empty;
        public string RawTin { get; set; } = string.Empty;
        public string RegisteredBusinessName { get; set; } = string.Empty;
        public string TaxpayerType { get; set; } = "Corporate / Domestic Corporation";
        public string RdoCode { get; set; } = string.Empty;
        public string RdoLocation { get; set; } = string.Empty;
        public string TaxpayerStatus { get; set; } = "ACTIVE"; // ACTIVE, SUSPENDED, CANCELLED, NOT_FOUND
        public bool NameMatchesSupplier { get; set; }
        public int NameMatchScorePercentage { get; set; }
        public bool HasValidTaxClearance { get; set; }
        public string TaxClearanceNumber { get; set; } = string.Empty;
        public DateTime? TaxClearanceValidUntil { get; set; }
        public DateTime VerifiedAtUtc { get; set; } = DateTime.UtcNow;
        public string VerificationSource { get; set; } = "LookupTax Global Tax ID Verification API (PH)";
        public string? Message { get; set; }
    }

    public interface IBirTinVerificationService
    {
        Task<BirTinVerificationResult> VerifyTinAsync(string tin, string? supplierName = null);
        string FormatTin(string rawTin);
        bool ValidateTinSyntax(string tin);
    }

    public interface ILookupTaxVerificationService : IBirTinVerificationService
    {
    }
}
