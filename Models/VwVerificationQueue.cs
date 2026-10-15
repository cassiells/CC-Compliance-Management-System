using System;
using System.Collections.Generic;

namespace DoComply.Models;

public partial class VwVerificationQueue
{
    public long DocumentSubmissionId { get; set; }

    public string SupplierName { get; set; } = null!;

    public string Document { get; set; } = null!;

    public string OriginalFileName { get; set; } = null!;

    public DateTime SubmittedAtUtc { get; set; }

    public string Status { get; set; } = null!;
}
