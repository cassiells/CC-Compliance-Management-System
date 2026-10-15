using System;
using System.Collections.Generic;

namespace DoComply.Models;

public partial class VwExpiredDocumentsReport
{
    public long SupplierId { get; set; }

    public string SupplierName { get; set; } = null!;

    public string Document { get; set; } = null!;

    public DateOnly? ExpiryDate { get; set; }
}
