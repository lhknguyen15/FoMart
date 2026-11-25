using System;
using System.Collections.Generic;

namespace FoMart.Domain.Entities;

public partial class PurchaseOrderLine
{
    public int Id { get; set; }

    public int PurchaseOrderId { get; set; }

    public int ProductId { get; set; }

    public int UnitId { get; set; }

    public int Qty { get; set; }

    public int ReceivedQty { get; set; }

    public virtual ProductUnit ProductUnit { get; set; } = null!;

    public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;
}
