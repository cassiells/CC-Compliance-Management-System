using System;
using System.Collections.Generic;

namespace DoComply.Models;

public partial class VwSupplierCompliance
{
    public int OrganizationId { get; set; }

    public long SupplierId { get; set; }

    public string SupplierName { get; set; } = null!;

    public long? RequirementCount { get; set; }

    public int? ValidCount { get; set; }

    public int? ExpiringSoonCount { get; set; }

    public int? PendingVerificationCount { get; set; }

    public int? MissingCount { get; set; }

    public int? RejectedCount { get; set; }

    public int? ExpiredCount { get; set; }

    public string ComplianceStatus { get; set; } = null!;
}
