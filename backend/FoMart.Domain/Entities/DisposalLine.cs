using System;
using System.Collections.Generic;

namespace FoMart.Domain.Entities;

public partial class DisposalLine
{
    public int Id { get; set; }

    public int DisposalId { get; set; }

    public int ProductId { get; set; }

    public int LocationId { get; set; }

    public int? LotId { get; set; }

    public int BaseQty { get; set; }

    public string Reason { get; set; } = null!;

    public string? Note { get; set; }

    public virtual Disposal Disposal { get; set; } = null!;

    public virtual Location Location { get; set; } = null!;

    public virtual Lot? Lot { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual ICollection<StockMove> StockMoves { get; set; } = new List<StockMove>();
}
