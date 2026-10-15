using System;
using System.Collections.Generic;

namespace DoComply.Models;

public partial class VwSupplierChecklist
{
    public int OrganizationId { get; set; }

    public long SupplierId { get; set; }

    public string SupplierName { get; set; } = null!;

    public int RequirementId { get; set; }

    public string RequirementName { get; set; } = null!;

    public bool ExpiryRequired { get; set; }

    public bool? IsSubmitted { get; set; }

    public long? DocumentSubmissionId { get; set; }

    public int? VersionNumber { get; set; }

    public string? OriginalFileName { get; set; }

    public DateTime? SubmittedAtUtc { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public string? EffectiveStatus { get; set; }
}
