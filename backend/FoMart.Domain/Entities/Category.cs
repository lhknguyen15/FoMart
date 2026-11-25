using System;
using System.Collections.Generic;

namespace FoMart.Domain.Entities;

public partial class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public short SortOrder { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public virtual ICollection<StockCount> StockCounts { get; set; } = new List<StockCount>();
}
