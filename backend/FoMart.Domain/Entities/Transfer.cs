using System;
using System.Collections.Generic;

namespace FoMart.Domain.Entities;

public partial class Transfer
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public int FromLocationId { get; set; }

    public int ToLocationId { get; set; }

    public bool IsAuto { get; set; }

    public Guid? SaleId { get; set; }

    public string? Note { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual Location FromLocation { get; set; } = null!;

    public virtual Sale? Sale { get; set; }

    public virtual Location ToLocation { get; set; } = null!;

    public virtual ICollection<TransferLine> TransferLines { get; set; } = new List<TransferLine>();
}
