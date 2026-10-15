using System;
using System.Collections.Generic;

namespace DoComply.Models;

public partial class Notification
{
    public long NotificationId { get; set; }

    public long UserId { get; set; }

    public long? SupplierId { get; set; }

    public long? DocumentSubmissionId { get; set; }

    public string NotificationType { get; set; } = null!;

    public string Message { get; set; } = null!;

    public bool IsRead { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ReadAtUtc { get; set; }

    public virtual DocumentSubmission? DocumentSubmission { get; set; }

    public virtual ICollection<NotificationDelivery> NotificationDeliveries { get; set; } = new List<NotificationDelivery>();

    public virtual Supplier? Supplier { get; set; }

    public virtual User User { get; set; } = null!;
}
