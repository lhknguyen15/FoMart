using System;
using System.Collections.Generic;

namespace FoMart.Domain.Entities;

public partial class User
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string? PasswordHash { get; set; }

    public string? PinHash { get; set; }

    public string Role { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual ICollection<Disposal> DisposalCreatedByNavigations { get; set; } = new List<Disposal>();

    public virtual ICollection<Disposal> DisposalDecidedByNavigations { get; set; } = new List<Disposal>();

    public virtual ICollection<GoodsReceipt> GoodsReceiptCancelledByNavigations { get; set; } = new List<GoodsReceipt>();

    public virtual ICollection<GoodsReceipt> GoodsReceiptCompletedByNavigations { get; set; } = new List<GoodsReceipt>();

    public virtual ICollection<GoodsReceipt> GoodsReceiptCreatedByNavigations { get; set; } = new List<GoodsReceipt>();

    public virtual ICollection<PriceHistory> PriceHistories { get; set; } = new List<PriceHistory>();

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public virtual ICollection<PurchaseOrder> PurchaseOrderCancelledByNavigations { get; set; } = new List<PurchaseOrder>();

    public virtual ICollection<PurchaseOrder> PurchaseOrderCreatedByNavigations { get; set; } = new List<PurchaseOrder>();

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public virtual ICollection<Sale> SaleCancelledByNavigations { get; set; } = new List<Sale>();

    public virtual ICollection<Sale> SaleCreatedByNavigations { get; set; } = new List<Sale>();

    public virtual ICollection<StockCount> StockCountApprovedByNavigations { get; set; } = new List<StockCount>();

    public virtual ICollection<StockCount> StockCountCreatedByNavigations { get; set; } = new List<StockCount>();

    public virtual ICollection<StockCount> StockCountSubmittedByNavigations { get; set; } = new List<StockCount>();

    public virtual ICollection<StockMove> StockMoves { get; set; } = new List<StockMove>();

    public virtual ICollection<Store> Stores { get; set; } = new List<Store>();

    public virtual ICollection<Transfer> Transfers { get; set; } = new List<Transfer>();
}
