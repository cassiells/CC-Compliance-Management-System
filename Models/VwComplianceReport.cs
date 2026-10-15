using System;
using System.Collections.Generic;

namespace DoComply.Models;

public partial class VwComplianceReport
{
    public long SupplierId { get; set; }

    public string SupplierName { get; set; } = null!;

    public string ComplianceStatus { get; set; } = null!;
}
