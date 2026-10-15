using System;
using System.Collections.Generic;

namespace DoComply.Models;

public partial class ProcurementRequest
{
    public long ProcurementRequestId { get; set; }

    public int OrganizationId { get; set; }

    public long SupplierId { get; set; }

    public long RequestedByUserId { get; set; }

    public string RequestName { get; set; } = null!;

    public string Purpose { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ApprovedAtUtc { get; set; }

    public long? ApprovedByUserId { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual User? ApprovedByUser { get; set; }

    public virtual Organization Organization { get; set; } = null!;

    public virtual User RequestedByUser { get; set; } = null!;

    public virtual Supplier Supplier { get; set; } = null!;
}
