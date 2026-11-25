using System;
using System.Collections.Generic;

namespace FoMart.Domain.Entities;

public partial class GoodsReceipt
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public int? SupplierId { get; set; }

    public int? PurchaseOrderId { get; set; }

    public int LocationId { get; set; }

    public string Status { get; set; } = null!;

    public long Total { get; set; }

    public string? Note { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? CompletedBy { get; set; }

    public DateTime? CompletedAt { get; set; }

    public int? CancelledBy { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? CancelReason { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual User? CancelledByNavigation { get; set; }

    public virtual User? CompletedByNavigation { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual Location Location { get; set; } = null!;

    public virtual PurchaseOrder? PurchaseOrder { get; set; }

    public virtual ICollection<ReceiptLine> ReceiptLines { get; set; } = new List<ReceiptLine>();

    public virtual Supplier? Supplier { get; set; }
}
