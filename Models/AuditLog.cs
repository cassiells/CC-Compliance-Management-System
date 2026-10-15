using System;
using System.Collections.Generic;

namespace DoComply.Models;

public partial class AuditLog
{
    public long AuditLogId { get; set; }

    public int OrganizationId { get; set; }

    public long? UserId { get; set; }

    public string UserDisplayName { get; set; } = null!;

    public string Action { get; set; } = null!;

    public string Module { get; set; } = null!;

    public string? EntityType { get; set; }

    public long? EntityId { get; set; }

    public string? DetailsJson { get; set; }

    public DateTime OccurredAtUtc { get; set; }

    public virtual Organization Organization { get; set; } = null!;

    public virtual User? User { get; set; }
}
