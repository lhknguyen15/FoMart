using System;
using System.Collections.Generic;

namespace FoMart.Domain.Entities;

public partial class VStockLedgerMismatch
{
    public int? LotId { get; set; }

    public int? LocationId { get; set; }

    public int? StockQty { get; set; }

    public int? LedgerQty { get; set; }
}
