using System;
using System.Collections.Generic;

namespace DoComply.Models;

public partial class SupplierRequirementAssignment
{
    public long SupplierId { get; set; }

    public int RequirementId { get; set; }

    public DateTime AssignedAtUtc { get; set; }

    public long? AssignedByUserId { get; set; }

    public bool IsRequired { get; set; }

    public virtual User? AssignedByUser { get; set; }

    public virtual Requirement Requirement { get; set; } = null!;

    public virtual Supplier Supplier { get; set; } = null!;
}
