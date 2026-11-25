using System;
using System.Collections.Generic;

namespace FoMart.Domain.Entities;

public partial class Lot
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string Kind { get; set; } = null!;

    public DateOnly? ExpiryDate { get; set; }

    public string? LotNo { get; set; }

    public decimal? UnitCost { get; set; }

    public DateTime ReceivedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<DisposalLine> DisposalLines { get; set; } = new List<DisposalLine>();

    public virtual Product Product { get; set; } = null!;

    public virtual ICollection<ReceiptLine> ReceiptLines { get; set; } = new List<ReceiptLine>();

    public virtual ICollection<StockMove> StockMoves { get; set; } = new List<StockMove>();

    public virtual ICollection<Stock> Stocks { get; set; } = new List<Stock>();
}
