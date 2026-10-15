using System;
using System.Collections.Generic;

namespace DoComply.Models;

public partial class Supplier
{
    public long SupplierId { get; set; }

    public int OrganizationId { get; set; }

    public string SupplierName { get; set; } = null!;

    public string ContactName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime RegisteredAtUtc { get; set; }

    public long? CreatedByUserId { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual User? CreatedByUser { get; set; }

    public virtual ICollection<DocumentSubmission> DocumentSubmissions { get; set; } = new List<DocumentSubmission>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual Organization Organization { get; set; } = null!;

    public virtual ICollection<ProcurementRequest> ProcurementRequests { get; set; } = new List<ProcurementRequest>();

    public virtual ICollection<SupplierRequirementAssignment> SupplierRequirementAssignments { get; set; } = new List<SupplierRequirementAssignment>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
