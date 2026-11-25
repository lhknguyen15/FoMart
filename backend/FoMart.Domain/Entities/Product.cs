using System;
using System.Collections.Generic;

namespace FoMart.Domain.Entities;

public partial class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int? CategoryId { get; set; }

    public int? DefaultSupplierId { get; set; }

    public bool TrackExpiry { get; set; }

    public int MinStock { get; set; }

    public int MinShelfStock { get; set; }

    public string Status { get; set; } = null!;

    public bool NeedsCount { get; set; }

    public DateTime? LastCountedAt { get; set; }

    public short? QuickButtonOrder { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual Category? Category { get; set; }

    public virtual ICollection<CountLine> CountLines { get; set; } = new List<CountLine>();

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual Supplier? DefaultSupplier { get; set; }

    public virtual ICollection<DisposalLine> DisposalLines { get; set; } = new List<DisposalLine>();

    public virtual Lot? Lot { get; set; }

    public virtual ProductUnit? ProductUnit { get; set; }

    public virtual ICollection<TransferLine> TransferLines { get; set; } = new List<TransferLine>();
}
