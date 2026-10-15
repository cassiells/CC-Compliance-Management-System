using System;
using System.Collections.Generic;

namespace DoComply.Models;

public partial class DocumentSubmission
{
    public long DocumentSubmissionId { get; set; }

    public long SupplierId { get; set; }

    public int RequirementId { get; set; }

    public int VersionNumber { get; set; }

    public string OriginalFileName { get; set; } = null!;

    public string StorageObjectKey { get; set; } = null!;

    public string StorageProvider { get; set; } = null!;

    public long? StorageVersion { get; set; }

    public string? StorageFormat { get; set; }

    public string StorageResourceType { get; set; } = null!;

    public string ContentType { get; set; } = null!;

    public long FileSizeBytes { get; set; }

    public byte[] FileSha256 { get; set; } = null!;

    public DateOnly? ExpiryDate { get; set; }

    public string Status { get; set; } = null!;

    public long SubmittedByUserId { get; set; }

    public DateTime SubmittedAtUtc { get; set; }

    public DateTime? ReviewedAtUtc { get; set; }

    public long? ReviewedByUserId { get; set; }

    public string? RejectionReason { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual Requirement Requirement { get; set; } = null!;

    public virtual User? ReviewedByUser { get; set; }

    public virtual User SubmittedByUser { get; set; } = null!;

    public virtual Supplier Supplier { get; set; } = null!;

    public virtual ICollection<VerificationReview> VerificationReviews { get; set; } = new List<VerificationReview>();
}
