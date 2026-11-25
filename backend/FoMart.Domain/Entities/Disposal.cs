using System;
using System.Collections.Generic;

namespace FoMart.Domain.Entities;

public partial class Disposal
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? Note { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? DecidedBy { get; set; }

    public DateTime? DecidedAt { get; set; }

    public string? RejectReason { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual User? DecidedByNavigation { get; set; }

    public virtual ICollection<DisposalLine> DisposalLines { get; set; } = new List<DisposalLine>();
}
