using System;
using System.Collections.Generic;

namespace FoMart.Domain.Entities;

public partial class VProductStock
{
    public int ProductId { get; set; }

    public int LocationId { get; set; }

    public int? Qty { get; set; }
}
