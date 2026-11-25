using System;
using System.Collections.Generic;

namespace FoMart.Domain.Entities;

public partial class StockMove
{
    public long Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public int ProductId { get; set; }

    public int LotId { get; set; }

    public int LocationId { get; set; }

    public int QtyChange { get; set; }

    public decimal? UnitCost { get; set; }

    public string DocType { get; set; } = null!;

    public int? ReceiptLineId { get; set; }

    public long? SaleLineId { get; set; }

    public int? TransferLineId { get; set; }

    public int? DisposalLineId { get; set; }

    public int? CountLineId { get; set; }

    public int UserId { get; set; }

    public virtual CountLine? CountLine { get; set; }

    public virtual DisposalLine? DisposalLine { get; set; }

    public virtual Location Location { get; set; } = null!;

    public virtual Lot Lot { get; set; } = null!;

    public virtual ReceiptLine? ReceiptLine { get; set; }

    public virtual SaleLine? SaleLine { get; set; }

    public virtual TransferLine? TransferLine { get; set; }

    public virtual User User { get; set; } = null!;
}
