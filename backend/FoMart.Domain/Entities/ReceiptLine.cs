using System;
using System.Collections.Generic;

namespace FoMart.Domain.Entities;

public partial class ReceiptLine
{
    public int Id { get; set; }

    public int ReceiptId { get; set; }

    public short LineNo { get; set; }

    public int ProductId { get; set; }

    public int UnitId { get; set; }

    public int Qty { get; set; }

    public int BaseQty { get; set; }

    public long? UnitPrice { get; set; }

    public long? LineTotal { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public string? LotNo { get; set; }

    public int? LotId { get; set; }

    public virtual Lot? Lot { get; set; }

    public virtual ProductUnit ProductUnit { get; set; } = null!;

    public virtual GoodsReceipt Receipt { get; set; } = null!;

    public virtual ICollection<StockMove> StockMoves { get; set; } = new List<StockMove>();
}
