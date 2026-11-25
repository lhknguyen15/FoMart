using System;
using System.Collections.Generic;

namespace FoMart.Domain.Entities;

public partial class PurchaseOrder
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public int SupplierId { get; set; }

    public string Status { get; set; } = null!;

    public string? Note { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? CancelledBy { get; set; }

    public DateTime? CancelledAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual User? CancelledByNavigation { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<GoodsReceipt> GoodsReceipts { get; set; } = new List<GoodsReceipt>();

    public virtual ICollection<PurchaseOrderLine> PurchaseOrderLines { get; set; } = new List<PurchaseOrderLine>();

    public virtual Supplier Supplier { get; set; } = null!;
}
