using System;
using System.Collections.Generic;

namespace DoComply.Models;

public partial class Organization
{
    public int OrganizationId { get; set; }

    public string OrganizationName { get; set; } = null!;

    public string? SupportEmail { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual NotificationSetting? NotificationSetting { get; set; }

    public virtual PasswordPolicy? PasswordPolicy { get; set; }

    public virtual ICollection<ProcurementRequest> ProcurementRequests { get; set; } = new List<ProcurementRequest>();

    public virtual ICollection<Requirement> Requirements { get; set; } = new List<Requirement>();

    public virtual ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
