using System;
using System.Collections.Generic;

namespace FoMart.Domain.Entities;

public partial class CountLine
{
    public int Id { get; set; }

    public int CountId { get; set; }

    public int ProductId { get; set; }

    public int LocationId { get; set; }

    public int SystemQty { get; set; }

    public int? MovesDuring { get; set; }

    public int? CountedQty { get; set; }

    public int? Diff { get; set; }

    public string? Reason { get; set; }

    public decimal? AdjustUnitCost { get; set; }

    public DateOnly? AdjustExpiryDate { get; set; }

    public string? Note { get; set; }

    public virtual StockCount Count { get; set; } = null!;

    public virtual Location Location { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;

    public virtual ICollection<StockMove> StockMoves { get; set; } = new List<StockMove>();
}
