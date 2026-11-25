using System;
using System.Collections.Generic;

namespace FoMart.Domain.Entities;

public partial class PriceHistory
{
    public long Id { get; set; }

    public int UnitId { get; set; }

    public long? OldPrice { get; set; }

    public long NewPrice { get; set; }

    public int ChangedBy { get; set; }

    public DateTime ChangedAt { get; set; }

    public virtual User ChangedByNavigation { get; set; } = null!;

    public virtual ProductUnit Unit { get; set; } = null!;
}
