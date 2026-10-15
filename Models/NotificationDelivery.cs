using System;
using System.Collections.Generic;

namespace DoComply.Models;

public partial class NotificationDelivery
{
    public long NotificationDeliveryId { get; set; }

    public long NotificationId { get; set; }

    public string Channel { get; set; } = null!;

    public string RecipientAddress { get; set; } = null!;

    public string DeliveryStatus { get; set; } = null!;

    public string Provider { get; set; } = null!;

    public string? ProviderMessageId { get; set; }

    public int AttemptCount { get; set; }

    public DateTime? LastAttemptAtUtc { get; set; }

    public DateTime? SentAtUtc { get; set; }

    public DateTime? DeliveredAtUtc { get; set; }

    public DateTime? FailedAtUtc { get; set; }

    public string? FailureReason { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual Notification Notification { get; set; } = null!;
}
