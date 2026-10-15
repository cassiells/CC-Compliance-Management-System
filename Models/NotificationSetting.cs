using System;
using System.Collections.Generic;

namespace DoComply.Models;

public partial class NotificationSetting
{
    public int OrganizationId { get; set; }

    public bool ExpirationRemindersEnabled { get; set; }

    public bool ExpiredDocumentAlertsEnabled { get; set; }

    public bool VerificationOutcomesEnabled { get; set; }

    public long? UpdatedByUserId { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public virtual Organization Organization { get; set; } = null!;

    public virtual User? UpdatedByUser { get; set; }
}
