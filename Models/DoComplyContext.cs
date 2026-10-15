using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace DoComply.Models;

public partial class DoComplyContext : DbContext
{
    public DoComplyContext()
    {
    }

    public DoComplyContext(DbContextOptions<DoComplyContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<DocumentSubmission> DocumentSubmissions { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<NotificationDelivery> NotificationDeliveries { get; set; }

    public virtual DbSet<NotificationSetting> NotificationSettings { get; set; }

    public virtual DbSet<Organization> Organizations { get; set; }

    public virtual DbSet<OutboxMessage> OutboxMessages { get; set; }

    public virtual DbSet<PasswordPolicy> PasswordPolicies { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<ProcurementRequest> ProcurementRequests { get; set; }

    public virtual DbSet<Requirement> Requirements { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<SupplierRequirementAssignment> SupplierRequirementAssignments { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<VerificationReview> VerificationReviews { get; set; }

    public virtual DbSet<VwComplianceReport> VwComplianceReports { get; set; }

    public virtual DbSet<VwExpiredDocumentsReport> VwExpiredDocumentsReports { get; set; }

    public virtual DbSet<VwExpiringDocumentsReport> VwExpiringDocumentsReports { get; set; }

    public virtual DbSet<VwSupplierChecklist> VwSupplierChecklists { get; set; }

    public virtual DbSet<VwSupplierCompliance> VwSupplierCompliances { get; set; }

    public virtual DbSet<VwVerificationQueue> VwVerificationQueues { get; set; }

//    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
//        => optionsBuilder.UseSqlServer("Data Source=DENSHIN\\SQLEXPRESS;Initial Catalog=DoComply;Integrated Security=True;Trust Server Certificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLog");

            entity.HasIndex(e => new { e.OrganizationId, e.OccurredAtUtc }, "IX_AuditLog_Organization_Date").IsDescending(false, true);

            entity.Property(e => e.Action).HasMaxLength(250);
            entity.Property(e => e.EntityType)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Module)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.OccurredAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UserDisplayName).HasMaxLength(200);

            entity.HasOne(d => d.Organization).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.OrganizationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AuditLog_Organizations");

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_AuditLog_Users");
        });

        modelBuilder.Entity<DocumentSubmission>(entity =>
        {
            entity.HasIndex(e => new { e.ExpiryDate, e.Status }, "IX_Documents_ExpiryMonitoring").HasFilter("([ExpiryDate] IS NOT NULL)");

            entity.HasIndex(e => new { e.SupplierId, e.RequirementId, e.VersionNumber }, "IX_Documents_LatestSubmission").IsDescending(false, false, true);

            entity.HasIndex(e => new { e.Status, e.SubmittedAtUtc }, "IX_Documents_VerificationQueue").HasFilter("([Status]='Pending Verification')");

            entity.HasIndex(e => new { e.SupplierId, e.RequirementId, e.VersionNumber }, "UQ_Documents_Supplier_Requirement_Version").IsUnique();

            entity.Property(e => e.ContentType).HasMaxLength(150);
            entity.Property(e => e.FileSha256)
                .HasMaxLength(32)
                .IsFixedLength();
            entity.Property(e => e.OriginalFileName).HasMaxLength(260);
            entity.Property(e => e.RejectionReason).HasMaxLength(1000);
            entity.Property(e => e.ReviewedAtUtc).HasPrecision(0);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.StorageFormat)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.StorageObjectKey).HasMaxLength(1000);
            entity.Property(e => e.StorageProvider)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("Cloudinary");
            entity.Property(e => e.StorageResourceType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("raw");
            entity.Property(e => e.SubmittedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UpdatedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Requirement).WithMany(p => p.DocumentSubmissions)
                .HasForeignKey(d => d.RequirementId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Documents_Requirements");

            entity.HasOne(d => d.ReviewedByUser).WithMany(p => p.DocumentSubmissionReviewedByUsers)
                .HasForeignKey(d => d.ReviewedByUserId)
                .HasConstraintName("FK_Documents_ReviewedBy");

            entity.HasOne(d => d.SubmittedByUser).WithMany(p => p.DocumentSubmissionSubmittedByUsers)
                .HasForeignKey(d => d.SubmittedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Documents_SubmittedBy");

            entity.HasOne(d => d.Supplier).WithMany(p => p.DocumentSubmissions)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Documents_Suppliers");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.IsRead, e.CreatedAtUtc }, "IX_Notifications_User_Unread").IsDescending(false, false, true);

            entity.Property(e => e.CreatedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Message).HasMaxLength(1000);
            entity.Property(e => e.NotificationType)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.ReadAtUtc).HasPrecision(0);

            entity.HasOne(d => d.DocumentSubmission).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.DocumentSubmissionId)
                .HasConstraintName("FK_Notifications_Documents");

            entity.HasOne(d => d.Supplier).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.SupplierId)
                .HasConstraintName("FK_Notifications_Suppliers");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Notifications_Users");
        });

        modelBuilder.Entity<NotificationDelivery>(entity =>
        {
            entity.HasIndex(e => new { e.DeliveryStatus, e.CreatedAtUtc }, "IX_NotificationDeliveries_WorkQueue");

            entity.HasIndex(e => new { e.Provider, e.ProviderMessageId }, "UX_NotificationDeliveries_ProviderMessageId")
                .IsUnique()
                .HasFilter("([ProviderMessageId] IS NOT NULL)");

            entity.Property(e => e.Channel)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Email");
            entity.Property(e => e.CreatedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.DeliveredAtUtc).HasPrecision(0);
            entity.Property(e => e.DeliveryStatus)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("Pending");
            entity.Property(e => e.FailedAtUtc).HasPrecision(0);
            entity.Property(e => e.FailureReason).HasMaxLength(1000);
            entity.Property(e => e.LastAttemptAtUtc).HasPrecision(0);
            entity.Property(e => e.Provider)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("Brevo");
            entity.Property(e => e.ProviderMessageId).HasMaxLength(200);
            entity.Property(e => e.RecipientAddress).HasMaxLength(320);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.SentAtUtc).HasPrecision(0);
            entity.Property(e => e.UpdatedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Notification).WithMany(p => p.NotificationDeliveries)
                .HasForeignKey(d => d.NotificationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NotificationDeliveries_Notifications");
        });

        modelBuilder.Entity<NotificationSetting>(entity =>
        {
            entity.HasKey(e => e.OrganizationId);

            entity.Property(e => e.OrganizationId).ValueGeneratedNever();
            entity.Property(e => e.ExpirationRemindersEnabled).HasDefaultValue(true);
            entity.Property(e => e.ExpiredDocumentAlertsEnabled).HasDefaultValue(true);
            entity.Property(e => e.UpdatedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.VerificationOutcomesEnabled).HasDefaultValue(true);

            entity.HasOne(d => d.Organization).WithOne(p => p.NotificationSetting)
                .HasForeignKey<NotificationSetting>(d => d.OrganizationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NotificationSettings_Organizations");

            entity.HasOne(d => d.UpdatedByUser).WithMany(p => p.NotificationSettings)
                .HasForeignKey(d => d.UpdatedByUserId)
                .HasConstraintName("FK_NotificationSettings_UpdatedBy");
        });

        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasIndex(e => e.OrganizationName, "UQ_Organizations_Name").IsUnique();

            entity.Property(e => e.CreatedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.OrganizationName).HasMaxLength(200);
            entity.Property(e => e.SupportEmail).HasMaxLength(320);
            entity.Property(e => e.UpdatedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasIndex(e => new { e.ProcessedAtUtc, e.CreatedAtUtc }, "IX_OutboxMessages_Unprocessed").HasFilter("([ProcessedAtUtc] IS NULL)");

            entity.Property(e => e.OutboxMessageId).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.AggregateId).HasMaxLength(100);
            entity.Property(e => e.AggregateType)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.LastError).HasMaxLength(2000);
            entity.Property(e => e.LockedAtUtc).HasPrecision(0);
            entity.Property(e => e.MessageType)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ProcessedAtUtc).HasPrecision(0);
        });

        modelBuilder.Entity<PasswordPolicy>(entity =>
        {
            entity.HasKey(e => e.OrganizationId);

            entity.Property(e => e.OrganizationId).ValueGeneratedNever();
            entity.Property(e => e.MinimumLength).HasDefaultValue((byte)12);
            entity.Property(e => e.RequireLowercase).HasDefaultValue(true);
            entity.Property(e => e.RequireNumber).HasDefaultValue(true);
            entity.Property(e => e.RequireSpecial).HasDefaultValue(true);
            entity.Property(e => e.RequireUppercase).HasDefaultValue(true);
            entity.Property(e => e.UpdatedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Organization).WithOne(p => p.PasswordPolicy)
                .HasForeignKey<PasswordPolicy>(d => d.OrganizationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PasswordPolicies_Organizations");
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasIndex(e => e.PermissionCode, "UQ_Permissions_Code").IsUnique();

            entity.Property(e => e.PermissionCode)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PermissionName).HasMaxLength(200);
        });

        modelBuilder.Entity<ProcurementRequest>(entity =>
        {
            entity.HasIndex(e => new { e.OrganizationId, e.Status, e.CreatedAtUtc }, "IX_ProcurementRequests_Status").IsDescending(false, false, true);

            entity.Property(e => e.ApprovedAtUtc).HasPrecision(0);
            entity.Property(e => e.CreatedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Purpose).HasMaxLength(2000);
            entity.Property(e => e.RequestName).HasMaxLength(250);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("Pending Approval");

            entity.HasOne(d => d.ApprovedByUser).WithMany(p => p.ProcurementRequestApprovedByUsers)
                .HasForeignKey(d => d.ApprovedByUserId)
                .HasConstraintName("FK_ProcurementRequests_ApprovedBy");

            entity.HasOne(d => d.Organization).WithMany(p => p.ProcurementRequests)
                .HasForeignKey(d => d.OrganizationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProcurementRequests_Organizations");

            entity.HasOne(d => d.RequestedByUser).WithMany(p => p.ProcurementRequestRequestedByUsers)
                .HasForeignKey(d => d.RequestedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProcurementRequests_RequestedBy");

            entity.HasOne(d => d.Supplier).WithMany(p => p.ProcurementRequests)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProcurementRequests_Suppliers");
        });

        modelBuilder.Entity<Requirement>(entity =>
        {
            entity.HasIndex(e => new { e.OrganizationId, e.RequirementName }, "UQ_Requirements_Organization_Name").IsUnique();

            entity.Property(e => e.AppliesTo)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("Supplier");
            entity.Property(e => e.CreatedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.RequirementName).HasMaxLength(200);
            entity.Property(e => e.UpdatedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.Requirements)
                .HasForeignKey(d => d.CreatedByUserId)
                .HasConstraintName("FK_Requirements_CreatedBy");

            entity.HasOne(d => d.Organization).WithMany(p => p.Requirements)
                .HasForeignKey(d => d.OrganizationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Requirements_Organizations");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(e => e.RoleName, "UQ_Roles_RoleName").IsUnique();

            entity.Property(e => e.RoleId).ValueGeneratedOnAdd();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.RoleName).HasMaxLength(50);

            entity.HasMany(d => d.Permissions).WithMany(p => p.Roles)
                .UsingEntity<Dictionary<string, object>>(
                    "RolePermission",
                    r => r.HasOne<Permission>().WithMany()
                        .HasForeignKey("PermissionId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_RolePermissions_Permissions"),
                    l => l.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_RolePermissions_Roles"),
                    j =>
                    {
                        j.HasKey("RoleId", "PermissionId");
                        j.ToTable("RolePermissions");
                    });
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasIndex(e => new { e.OrganizationId, e.Status, e.SupplierName }, "IX_Suppliers_Status_Name");

            entity.HasIndex(e => new { e.OrganizationId, e.SupplierName }, "UQ_Suppliers_Organization_Name").IsUnique();

            entity.Property(e => e.ContactName).HasMaxLength(200);
            entity.Property(e => e.Email).HasMaxLength(320);
            entity.Property(e => e.RegisteredAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Active");
            entity.Property(e => e.SupplierName).HasMaxLength(250);
            entity.Property(e => e.UpdatedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.Suppliers)
                .HasForeignKey(d => d.CreatedByUserId)
                .HasConstraintName("FK_Suppliers_CreatedBy");

            entity.HasOne(d => d.Organization).WithMany(p => p.Suppliers)
                .HasForeignKey(d => d.OrganizationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Suppliers_Organizations");

            entity.HasMany(d => d.Users).WithMany(p => p.SuppliersNavigation)
                .UsingEntity<Dictionary<string, object>>(
                    "SupplierUser",
                    r => r.HasOne<User>().WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_SupplierUsers_Users"),
                    l => l.HasOne<Supplier>().WithMany()
                        .HasForeignKey("SupplierId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_SupplierUsers_Suppliers"),
                    j =>
                    {
                        j.HasKey("SupplierId", "UserId");
                        j.ToTable("SupplierUsers");
                    });
        });

        modelBuilder.Entity<SupplierRequirementAssignment>(entity =>
        {
            entity.HasKey(e => new { e.SupplierId, e.RequirementId });

            entity.Property(e => e.AssignedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IsRequired).HasDefaultValue(true);

            entity.HasOne(d => d.AssignedByUser).WithMany(p => p.SupplierRequirementAssignments)
                .HasForeignKey(d => d.AssignedByUserId)
                .HasConstraintName("FK_Assignments_AssignedBy");

            entity.HasOne(d => d.Requirement).WithMany(p => p.SupplierRequirementAssignments)
                .HasForeignKey(d => d.RequirementId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Assignments_Requirements");

            entity.HasOne(d => d.Supplier).WithMany(p => p.SupplierRequirementAssignments)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Assignments_Suppliers");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => new { e.RoleId, e.Status }, "IX_Users_Role_Status");

            entity.HasIndex(e => new { e.OrganizationId, e.Username }, "UX_Users_Organization_Username").IsUnique();

            entity.Property(e => e.CreatedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.FullName).HasMaxLength(200);
            entity.Property(e => e.LastLoginAtUtc).HasPrecision(0);
            entity.Property(e => e.MustChangePassword).HasDefaultValue(true);
            entity.Property(e => e.PasswordChangedAtUtc).HasPrecision(0);
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Active");
            entity.Property(e => e.UpdatedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Username).HasMaxLength(320);

            entity.HasOne(d => d.Organization).WithMany(p => p.Users)
                .HasForeignKey(d => d.OrganizationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Organizations");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Roles");
        });

        modelBuilder.Entity<VerificationReview>(entity =>
        {
            entity.Property(e => e.Decision)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.RejectionReason).HasMaxLength(1000);
            entity.Property(e => e.ReviewedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.AdministratorUser).WithMany(p => p.VerificationReviews)
                .HasForeignKey(d => d.AdministratorUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VerificationReviews_Administrator");

            entity.HasOne(d => d.DocumentSubmission).WithMany(p => p.VerificationReviews)
                .HasForeignKey(d => d.DocumentSubmissionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VerificationReviews_Document");
        });

        modelBuilder.Entity<VwComplianceReport>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_ComplianceReport");

            entity.Property(e => e.ComplianceStatus)
                .HasMaxLength(13)
                .IsUnicode(false);
            entity.Property(e => e.SupplierName).HasMaxLength(250);
        });

        modelBuilder.Entity<VwExpiredDocumentsReport>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_ExpiredDocumentsReport");

            entity.Property(e => e.Document).HasMaxLength(200);
            entity.Property(e => e.SupplierName).HasMaxLength(250);
        });

        modelBuilder.Entity<VwExpiringDocumentsReport>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_ExpiringDocumentsReport");

            entity.Property(e => e.Document).HasMaxLength(200);
            entity.Property(e => e.SupplierName).HasMaxLength(250);
        });

        modelBuilder.Entity<VwSupplierChecklist>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_SupplierChecklist");

            entity.Property(e => e.EffectiveStatus)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.OriginalFileName).HasMaxLength(260);
            entity.Property(e => e.RequirementName).HasMaxLength(200);
            entity.Property(e => e.SubmittedAtUtc).HasPrecision(0);
            entity.Property(e => e.SupplierName).HasMaxLength(250);
        });

        modelBuilder.Entity<VwSupplierCompliance>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_SupplierCompliance");

            entity.Property(e => e.ComplianceStatus)
                .HasMaxLength(13)
                .IsUnicode(false);
            entity.Property(e => e.SupplierName).HasMaxLength(250);
        });

        modelBuilder.Entity<VwVerificationQueue>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_VerificationQueue");

            entity.Property(e => e.Document).HasMaxLength(200);
            entity.Property(e => e.OriginalFileName).HasMaxLength(260);
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.SubmittedAtUtc).HasPrecision(0);
            entity.Property(e => e.SupplierName).HasMaxLength(250);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
