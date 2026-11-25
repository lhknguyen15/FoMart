using System;
using System.Collections.Generic;

namespace FoMart.Domain.Entities;

public partial class Location
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Type { get; set; } = null!;

    public bool IsDefaultSale { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<CountLine> CountLines { get; set; } = new List<CountLine>();

    public virtual ICollection<DisposalLine> DisposalLines { get; set; } = new List<DisposalLine>();

    public virtual ICollection<GoodsReceipt> GoodsReceipts { get; set; } = new List<GoodsReceipt>();

    public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();

    public virtual ICollection<StockCount> StockCounts { get; set; } = new List<StockCount>();

    public virtual ICollection<StockMove> StockMoves { get; set; } = new List<StockMove>();

    public virtual ICollection<Stock> Stocks { get; set; } = new List<Stock>();

    public virtual ICollection<Transfer> TransferFromLocations { get; set; } = new List<Transfer>();

    public virtual ICollection<Transfer> TransferToLocations { get; set; } = new List<Transfer>();
}
