using System;
using System.Collections.Generic;

namespace FoMart.Domain.Entities;

public partial class Stock
{
    public int LotId { get; set; }

    public int LocationId { get; set; }

    public int ProductId { get; set; }

    public int Qty { get; set; }

    public DateTime UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual Location Location { get; set; } = null!;

    public virtual Lot Lot { get; set; } = null!;
}
