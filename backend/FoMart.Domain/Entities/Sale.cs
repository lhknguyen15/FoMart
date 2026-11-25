using System;
using System.Collections.Generic;

namespace FoMart.Domain.Entities;

public partial class Sale
{
    public Guid Id { get; set; }

    public long Seq { get; set; }

    public string Code { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string PaymentMethod { get; set; } = null!;

    public int LocationId { get; set; }

    public long Total { get; set; }

    public long? CashGiven { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime SyncedAt { get; set; }

    public bool IsOffline { get; set; }

    public int? CancelledBy { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? CancelReason { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual User? CancelledByNavigation { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual Location Location { get; set; } = null!;

    public virtual ICollection<SaleLine> SaleLines { get; set; } = new List<SaleLine>();

    public virtual ICollection<Transfer> Transfers { get; set; } = new List<Transfer>();
}
