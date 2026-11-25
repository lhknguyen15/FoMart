using System;
using System.Collections.Generic;

namespace FoMart.Domain.Entities;

public partial class RefreshToken
{
    public long Id { get; set; }

    public int UserId { get; set; }

    public string TokenHash { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public long? ReplacedById { get; set; }

    public virtual ICollection<RefreshToken> InverseReplacedBy { get; set; } = new List<RefreshToken>();

    public virtual RefreshToken? ReplacedBy { get; set; }

    public virtual User User { get; set; } = null!;
}
