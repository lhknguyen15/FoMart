using System;
using System.Collections.Generic;

namespace FoMart.Domain.Entities;

public partial class StockCount
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string ScopeType { get; set; } = null!;

    public int? LocationId { get; set; }

    public int? CategoryId { get; set; }

    public string Status { get; set; } = null!;

    public string? Note { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? SubmittedBy { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public int? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual User? ApprovedByNavigation { get; set; }

    public virtual Category? Category { get; set; }

    public virtual ICollection<CountLine> CountLines { get; set; } = new List<CountLine>();

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual Location? Location { get; set; }

    public virtual User? SubmittedByNavigation { get; set; }
}
