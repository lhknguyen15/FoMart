using System;
using System.Collections.Generic;

namespace FoMart.Domain.Entities;

public partial class TransferLine
{
    public int Id { get; set; }

    public int TransferId { get; set; }

    public int ProductId { get; set; }

    public int BaseQty { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual ICollection<StockMove> StockMoves { get; set; } = new List<StockMove>();

    public virtual Transfer Transfer { get; set; } = null!;
}
