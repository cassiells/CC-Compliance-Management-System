using System;
using System.Collections.Generic;

namespace DoComply.Models;

public partial class PasswordPolicy
{
    public int OrganizationId { get; set; }

    public byte MinimumLength { get; set; }

    public bool RequireUppercase { get; set; }

    public bool RequireLowercase { get; set; }

    public bool RequireNumber { get; set; }

    public bool RequireSpecial { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public virtual Organization Organization { get; set; } = null!;
}
