using System;
using System.Collections.Generic;

namespace DoComply.Models;

public partial class VerificationReview
{
    public long VerificationReviewId { get; set; }

    public long DocumentSubmissionId { get; set; }

    public long AdministratorUserId { get; set; }

    public string Decision { get; set; } = null!;

    public string? RejectionReason { get; set; }

    public DateTime ReviewedAtUtc { get; set; }

    public virtual User AdministratorUser { get; set; } = null!;

    public virtual DocumentSubmission DocumentSubmission { get; set; } = null!;
}
