using System;
using System.Collections.Generic;

namespace FoMart.Domain.Entities;

public partial class Store
{
    public byte Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public string? BankBin { get; set; }

    public string? BankAccountNo { get; set; }

    public string? BankAccountName { get; set; }

    public short ExpiryWarnDays { get; set; }

    public short ReorderCoverDays { get; set; }

    public short SalesLookbackDays { get; set; }

    public short CountIntervalDays { get; set; }

    public bool StaffCanSeeCost { get; set; }

    public short IdleLockMinutes { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual User? UpdatedByNavigation { get; set; }
}
