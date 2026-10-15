using System;
using System.Collections.Generic;

namespace DoComply.Models;

public partial class Requirement
{
    public int RequirementId { get; set; }

    public int OrganizationId { get; set; }

    public string RequirementName { get; set; } = null!;

    public string AppliesTo { get; set; } = null!;

    public bool ExpiryRequired { get; set; }

    public bool IsActive { get; set; }

    public long? CreatedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public virtual User? CreatedByUser { get; set; }

    public virtual ICollection<DocumentSubmission> DocumentSubmissions { get; set; } = new List<DocumentSubmission>();

    public virtual Organization Organization { get; set; } = null!;

    public virtual ICollection<SupplierRequirementAssignment> SupplierRequirementAssignments { get; set; } = new List<SupplierRequirementAssignment>();
}
