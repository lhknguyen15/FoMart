using System;
using System.Collections.Generic;

namespace FoMart.Domain.Entities;

public partial class SaleLine
{
    public long Id { get; set; }

    public Guid SaleId { get; set; }

    public short LineNo { get; set; }

    public int? ProductId { get; set; }

    public int? UnitId { get; set; }

    public string NameSnapshot { get; set; } = null!;

    public int Qty { get; set; }

    public long UnitPrice { get; set; }

    public long LineTotal { get; set; }

    public int? BaseQty { get; set; }

    public decimal? CostTotal { get; set; }

    public bool ExpiredConfirmed { get; set; }

    public virtual ProductUnit? ProductUnit { get; set; }

    public virtual Sale Sale { get; set; } = null!;

    public virtual ICollection<StockMove> StockMoves { get; set; } = new List<StockMove>();
}
