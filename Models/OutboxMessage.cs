using System;
using System.Collections.Generic;

namespace DoComply.Models;

public partial class OutboxMessage
{
    public Guid OutboxMessageId { get; set; }

    public string MessageType { get; set; } = null!;

    public string AggregateType { get; set; } = null!;

    public string AggregateId { get; set; } = null!;

    public string PayloadJson { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? LockedAtUtc { get; set; }

    public Guid? LockId { get; set; }

    public DateTime? ProcessedAtUtc { get; set; }

    public int AttemptCount { get; set; }

    public string? LastError { get; set; }
}
