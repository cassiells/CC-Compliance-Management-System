using System;
using System.Collections.Generic;

namespace DoComply.Models;

public partial class User
{
    public long UserId { get; set; }

    public int OrganizationId { get; set; }

    public byte RoleId { get; set; }

    public string FullName { get; set; } = null!;

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Status { get; set; } = null!;

    public bool MustChangePassword { get; set; }

    public DateTime? PasswordChangedAtUtc { get; set; }

    public DateTime? LastLoginAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual ICollection<DocumentSubmission> DocumentSubmissionReviewedByUsers { get; set; } = new List<DocumentSubmission>();

    public virtual ICollection<DocumentSubmission> DocumentSubmissionSubmittedByUsers { get; set; } = new List<DocumentSubmission>();

    public virtual ICollection<NotificationSetting> NotificationSettings { get; set; } = new List<NotificationSetting>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual Organization Organization { get; set; } = null!;

    public virtual ICollection<ProcurementRequest> ProcurementRequestApprovedByUsers { get; set; } = new List<ProcurementRequest>();

    public virtual ICollection<ProcurementRequest> ProcurementRequestRequestedByUsers { get; set; } = new List<ProcurementRequest>();

    public virtual ICollection<Requirement> Requirements { get; set; } = new List<Requirement>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<SupplierRequirementAssignment> SupplierRequirementAssignments { get; set; } = new List<SupplierRequirementAssignment>();

    public virtual ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();

    public virtual ICollection<VerificationReview> VerificationReviews { get; set; } = new List<VerificationReview>();

    public virtual ICollection<Supplier> SuppliersNavigation { get; set; } = new List<Supplier>();
}
