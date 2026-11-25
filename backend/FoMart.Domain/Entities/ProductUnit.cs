using System;
using System.Collections.Generic;

namespace FoMart.Domain.Entities;

public partial class ProductUnit
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string Name { get; set; } = null!;

    public int Factor { get; set; }

    public string? Barcode { get; set; }

    public long SalePrice { get; set; }

    public bool IsBase { get; set; }

    public bool IsDefaultPurchase { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual ICollection<PriceHistory> PriceHistories { get; set; } = new List<PriceHistory>();

    public virtual Product Product { get; set; } = null!;

    public virtual ICollection<PurchaseOrderLine> PurchaseOrderLines { get; set; } = new List<PurchaseOrderLine>();

    public virtual ICollection<ReceiptLine> ReceiptLines { get; set; } = new List<ReceiptLine>();

    public virtual ICollection<SaleLine> SaleLines { get; set; } = new List<SaleLine>();
}
