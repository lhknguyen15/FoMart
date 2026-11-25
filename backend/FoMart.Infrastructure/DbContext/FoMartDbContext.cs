using System;
using System.Collections.Generic;
using FoMart.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoMart.Infrastructure;

public partial class FoMartDbContext : DbContext
{
    public FoMartDbContext(DbContextOptions<FoMartDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<CountLine> CountLines { get; set; }

    public virtual DbSet<Disposal> Disposals { get; set; }

    public virtual DbSet<DisposalLine> DisposalLines { get; set; }

    public virtual DbSet<GoodsReceipt> GoodsReceipts { get; set; }

    public virtual DbSet<Location> Locations { get; set; }

    public virtual DbSet<Lot> Lots { get; set; }

    public virtual DbSet<PriceHistory> PriceHistories { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductUnit> ProductUnits { get; set; }

    public virtual DbSet<PurchaseOrder> PurchaseOrders { get; set; }

    public virtual DbSet<PurchaseOrderLine> PurchaseOrderLines { get; set; }

    public virtual DbSet<ReceiptLine> ReceiptLines { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<Sale> Sales { get; set; }

    public virtual DbSet<SaleLine> SaleLines { get; set; }

    public virtual DbSet<Stock> Stocks { get; set; }

    public virtual DbSet<StockCount> StockCounts { get; set; }

    public virtual DbSet<StockMove> StockMoves { get; set; }

    public virtual DbSet<Store> Stores { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<Transfer> Transfers { get; set; }

    public virtual DbSet<TransferLine> TransferLines { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<VProductStock> VProductStocks { get; set; }

    public virtual DbSet<VStockLedgerMismatch> VStockLedgerMismatches { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories", "catalog");

            entity.HasIndex(e => e.Name, "UQ_categories_name").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true, "DF_categories_is_active")
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");
        });

        modelBuilder.Entity<CountLine>(entity =>
        {
            entity.ToTable("count_lines", "inventory");

            entity.HasIndex(e => new { e.CountId, e.ProductId, e.LocationId }, "UQ_count_lines_product_location").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AdjustExpiryDate).HasColumnName("adjust_expiry_date");
            entity.Property(e => e.AdjustUnitCost)
                .HasColumnType("decimal(14, 4)")
                .HasColumnName("adjust_unit_cost");
            entity.Property(e => e.CountId).HasColumnName("count_id");
            entity.Property(e => e.CountedQty).HasColumnName("counted_qty");
            entity.Property(e => e.Diff)
                .HasComputedColumnSql("([counted_qty]-([system_qty]+[moves_during]))", true)
                .HasColumnName("diff");
            entity.Property(e => e.LocationId).HasColumnName("location_id");
            entity.Property(e => e.MovesDuring).HasColumnName("moves_during");
            entity.Property(e => e.Note)
                .HasMaxLength(300)
                .HasColumnName("note");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Reason)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("reason");
            entity.Property(e => e.SystemQty).HasColumnName("system_qty");

            entity.HasOne(d => d.Count).WithMany(p => p.CountLines)
                .HasForeignKey(d => d.CountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_count_lines_count");

            entity.HasOne(d => d.Location).WithMany(p => p.CountLines)
                .HasForeignKey(d => d.LocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_count_lines_location");

            entity.HasOne(d => d.Product).WithMany(p => p.CountLines)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_count_lines_product");
        });

        modelBuilder.Entity<Disposal>(entity =>
        {
            entity.ToTable("disposals", "inventory");

            entity.HasIndex(e => e.CreatedAt, "IX_disposals_pending").HasFilter("([status]='pending')");

            entity.HasIndex(e => e.Code, "UQ_disposals_code").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasDefaultValueSql("('XH'+right('00000000'+CONVERT([varchar](10),NEXT VALUE FOR [inventory].[seq_disposal_code]),(8)))", "DF_disposals_code")
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_disposals_created_at")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.DecidedAt)
                .HasPrecision(3)
                .HasColumnName("decided_at");
            entity.Property(e => e.DecidedBy).HasColumnName("decided_by");
            entity.Property(e => e.Note)
                .HasMaxLength(500)
                .HasColumnName("note");
            entity.Property(e => e.RejectReason)
                .HasMaxLength(300)
                .HasColumnName("reject_reason");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken()
                .HasColumnName("row_version");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("pending", "DF_disposals_status")
                .HasColumnName("status");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.DisposalCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_disposals_created_by");

            entity.HasOne(d => d.DecidedByNavigation).WithMany(p => p.DisposalDecidedByNavigations)
                .HasForeignKey(d => d.DecidedBy)
                .HasConstraintName("FK_disposals_decided_by");
        });

        modelBuilder.Entity<DisposalLine>(entity =>
        {
            entity.ToTable("disposal_lines", "inventory");

            entity.HasIndex(e => e.DisposalId, "IX_disposal_lines_disposal");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BaseQty).HasColumnName("base_qty");
            entity.Property(e => e.DisposalId).HasColumnName("disposal_id");
            entity.Property(e => e.LocationId).HasColumnName("location_id");
            entity.Property(e => e.LotId).HasColumnName("lot_id");
            entity.Property(e => e.Note)
                .HasMaxLength(300)
                .HasColumnName("note");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Reason)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("reason");

            entity.HasOne(d => d.Disposal).WithMany(p => p.DisposalLines)
                .HasForeignKey(d => d.DisposalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_disposal_lines_disposal");

            entity.HasOne(d => d.Location).WithMany(p => p.DisposalLines)
                .HasForeignKey(d => d.LocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_disposal_lines_location");

            entity.HasOne(d => d.Product).WithMany(p => p.DisposalLines)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_disposal_lines_product");

            entity.HasOne(d => d.Lot).WithMany(p => p.DisposalLines)
                .HasPrincipalKey(p => new { p.Id, p.ProductId })
                .HasForeignKey(d => new { d.LotId, d.ProductId })
                .HasConstraintName("FK_disposal_lines_lot");
        });

        modelBuilder.Entity<GoodsReceipt>(entity =>
        {
            entity.ToTable("goods_receipts", "purchasing");

            entity.HasIndex(e => e.PurchaseOrderId, "IX_goods_receipts_order").HasFilter("([purchase_order_id] IS NOT NULL)");

            entity.HasIndex(e => new { e.Status, e.CreatedAt }, "IX_goods_receipts_status_created");

            entity.HasIndex(e => e.Code, "UQ_goods_receipts_code").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CancelReason)
                .HasMaxLength(300)
                .HasColumnName("cancel_reason");
            entity.Property(e => e.CancelledAt)
                .HasPrecision(3)
                .HasColumnName("cancelled_at");
            entity.Property(e => e.CancelledBy).HasColumnName("cancelled_by");
            entity.Property(e => e.Code)
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasDefaultValueSql("('PN'+right('00000000'+CONVERT([varchar](10),NEXT VALUE FOR [purchasing].[seq_goods_receipt_code]),(8)))", "DF_goods_receipts_code")
                .HasColumnName("code");
            entity.Property(e => e.CompletedAt)
                .HasPrecision(3)
                .HasColumnName("completed_at");
            entity.Property(e => e.CompletedBy).HasColumnName("completed_by");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_goods_receipts_created_at")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.LocationId).HasColumnName("location_id");
            entity.Property(e => e.Note)
                .HasMaxLength(500)
                .HasColumnName("note");
            entity.Property(e => e.PurchaseOrderId).HasColumnName("purchase_order_id");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken()
                .HasColumnName("row_version");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("draft", "DF_goods_receipts_status")
                .HasColumnName("status");
            entity.Property(e => e.SupplierId).HasColumnName("supplier_id");
            entity.Property(e => e.Total).HasColumnName("total");

            entity.HasOne(d => d.CancelledByNavigation).WithMany(p => p.GoodsReceiptCancelledByNavigations)
                .HasForeignKey(d => d.CancelledBy)
                .HasConstraintName("FK_goods_receipts_cancelled_by");

            entity.HasOne(d => d.CompletedByNavigation).WithMany(p => p.GoodsReceiptCompletedByNavigations)
                .HasForeignKey(d => d.CompletedBy)
                .HasConstraintName("FK_goods_receipts_completed_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.GoodsReceiptCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_goods_receipts_created_by");

            entity.HasOne(d => d.Location).WithMany(p => p.GoodsReceipts)
                .HasForeignKey(d => d.LocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_goods_receipts_location");

            entity.HasOne(d => d.PurchaseOrder).WithMany(p => p.GoodsReceipts)
                .HasForeignKey(d => d.PurchaseOrderId)
                .HasConstraintName("FK_goods_receipts_order");

            entity.HasOne(d => d.Supplier).WithMany(p => p.GoodsReceipts)
                .HasForeignKey(d => d.SupplierId)
                .HasConstraintName("FK_goods_receipts_supplier");
        });

        modelBuilder.Entity<Location>(entity =>
        {
            entity.ToTable("locations", "catalog");

            entity.HasIndex(e => e.Name, "UQ_locations_name").IsUnique();

            entity.HasIndex(e => e.IsDefaultSale, "UX_locations_default_sale")
                .IsUnique()
                .HasFilter("([is_default_sale]=(1))");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_locations_created_at")
                .HasColumnName("created_at");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true, "DF_locations_is_active")
                .HasColumnName("is_active");
            entity.Property(e => e.IsDefaultSale).HasColumnName("is_default_sale");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Type)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("type");
        });

        modelBuilder.Entity<Lot>(entity =>
        {
            entity.ToTable("lots", "inventory");

            entity.HasIndex(e => new { e.ProductId, e.ExpiryDate, e.ReceivedAt }, "IX_lots_product_expiry");

            entity.HasIndex(e => new { e.Id, e.ProductId }, "UQ_lots_id_product").IsUnique();

            entity.HasIndex(e => e.ProductId, "UX_lots_provisional")
                .IsUnique()
                .HasFilter("([kind]='provisional')");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_lots_created_at")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiryDate).HasColumnName("expiry_date");
            entity.Property(e => e.Kind)
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasColumnName("kind");
            entity.Property(e => e.LotNo)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("lot_no");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.ReceivedAt)
                .HasPrecision(3)
                .HasColumnName("received_at");
            entity.Property(e => e.UnitCost)
                .HasColumnType("decimal(14, 4)")
                .HasColumnName("unit_cost");

            entity.HasOne(d => d.Product).WithOne(p => p.Lot)
                .HasForeignKey<Lot>(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_lots_product");
        });

        modelBuilder.Entity<PriceHistory>(entity =>
        {
            entity.ToTable("price_history", "catalog");

            entity.HasIndex(e => new { e.UnitId, e.ChangedAt }, "IX_price_history_unit");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChangedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_price_history_changed_at")
                .HasColumnName("changed_at");
            entity.Property(e => e.ChangedBy).HasColumnName("changed_by");
            entity.Property(e => e.NewPrice).HasColumnName("new_price");
            entity.Property(e => e.OldPrice).HasColumnName("old_price");
            entity.Property(e => e.UnitId).HasColumnName("unit_id");

            entity.HasOne(d => d.ChangedByNavigation).WithMany(p => p.PriceHistories)
                .HasForeignKey(d => d.ChangedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_price_history_changed_by");

            entity.HasOne(d => d.Unit).WithMany(p => p.PriceHistories)
                .HasForeignKey(d => d.UnitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_price_history_unit");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products", "catalog");

            entity.HasIndex(e => e.CategoryId, "IX_products_category");

            entity.HasIndex(e => e.Name, "IX_products_name");

            entity.HasIndex(e => e.Id, "IX_products_needs_count").HasFilter("([needs_count]=(1))");

            entity.HasIndex(e => e.QuickButtonOrder, "UX_products_quick_button")
                .IsUnique()
                .HasFilter("([quick_button_order] IS NOT NULL)");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_products_created_at")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.DefaultSupplierId).HasColumnName("default_supplier_id");
            entity.Property(e => e.LastCountedAt)
                .HasPrecision(3)
                .HasColumnName("last_counted_at");
            entity.Property(e => e.MinShelfStock).HasColumnName("min_shelf_stock");
            entity.Property(e => e.MinStock).HasColumnName("min_stock");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");
            entity.Property(e => e.NeedsCount).HasColumnName("needs_count");
            entity.Property(e => e.QuickButtonOrder).HasColumnName("quick_button_order");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken()
                .HasColumnName("row_version");
            entity.Property(e => e.Status)
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasDefaultValue("active", "DF_products_status")
                .HasColumnName("status");
            entity.Property(e => e.TrackExpiry).HasColumnName("track_expiry");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK_products_category");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Products)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_products_created_by");

            entity.HasOne(d => d.DefaultSupplier).WithMany(p => p.Products)
                .HasForeignKey(d => d.DefaultSupplierId)
                .HasConstraintName("FK_products_default_supplier");
        });

        modelBuilder.Entity<ProductUnit>(entity =>
        {
            entity.ToTable("product_units", "catalog", tb => tb.HasTrigger("TR_product_units_lock_factor"));

            entity.HasIndex(e => new { e.Id, e.ProductId }, "UQ_product_units_id_product").IsUnique();

            entity.HasIndex(e => new { e.ProductId, e.Name }, "UQ_product_units_product_name").IsUnique();

            entity.HasIndex(e => e.Barcode, "UX_product_units_barcode")
                .IsUnique()
                .HasFilter("([barcode] IS NOT NULL)");

            entity.HasIndex(e => e.ProductId, "UX_product_units_base")
                .IsUnique()
                .HasFilter("([is_base]=(1))");

            entity.HasIndex(e => e.ProductId, "UX_product_units_default_purchase")
                .IsUnique()
                .HasFilter("([is_default_purchase]=(1))");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Barcode)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("barcode");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_product_units_created_at")
                .HasColumnName("created_at");
            entity.Property(e => e.Factor).HasColumnName("factor");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true, "DF_product_units_is_active")
                .HasColumnName("is_active");
            entity.Property(e => e.IsBase).HasColumnName("is_base");
            entity.Property(e => e.IsDefaultPurchase).HasColumnName("is_default_purchase");
            entity.Property(e => e.Name)
                .HasMaxLength(30)
                .HasColumnName("name");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken()
                .HasColumnName("row_version");
            entity.Property(e => e.SalePrice).HasColumnName("sale_price");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Product).WithOne(p => p.ProductUnit)
                .HasForeignKey<ProductUnit>(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_product_units_product");
        });

        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.ToTable("purchase_orders", "purchasing");

            entity.HasIndex(e => new { e.SupplierId, e.Status }, "IX_purchase_orders_supplier_status");

            entity.HasIndex(e => e.Code, "UQ_purchase_orders_code").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CancelledAt)
                .HasPrecision(3)
                .HasColumnName("cancelled_at");
            entity.Property(e => e.CancelledBy).HasColumnName("cancelled_by");
            entity.Property(e => e.Code)
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasDefaultValueSql("('DH'+right('00000000'+CONVERT([varchar](10),NEXT VALUE FOR [purchasing].[seq_purchase_order_code]),(8)))", "DF_purchase_orders_code")
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_purchase_orders_created_at")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Note)
                .HasMaxLength(500)
                .HasColumnName("note");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken()
                .HasColumnName("row_version");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("ordered", "DF_purchase_orders_status")
                .HasColumnName("status");
            entity.Property(e => e.SupplierId).HasColumnName("supplier_id");

            entity.HasOne(d => d.CancelledByNavigation).WithMany(p => p.PurchaseOrderCancelledByNavigations)
                .HasForeignKey(d => d.CancelledBy)
                .HasConstraintName("FK_purchase_orders_cancelled_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.PurchaseOrderCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_purchase_orders_created_by");

            entity.HasOne(d => d.Supplier).WithMany(p => p.PurchaseOrders)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_purchase_orders_supplier");
        });

        modelBuilder.Entity<PurchaseOrderLine>(entity =>
        {
            entity.ToTable("purchase_order_lines", "purchasing");

            entity.HasIndex(e => new { e.PurchaseOrderId, e.ProductId, e.UnitId }, "UQ_purchase_order_lines_unit").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.PurchaseOrderId).HasColumnName("purchase_order_id");
            entity.Property(e => e.Qty).HasColumnName("qty");
            entity.Property(e => e.ReceivedQty).HasColumnName("received_qty");
            entity.Property(e => e.UnitId).HasColumnName("unit_id");

            entity.HasOne(d => d.PurchaseOrder).WithMany(p => p.PurchaseOrderLines)
                .HasForeignKey(d => d.PurchaseOrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_purchase_order_lines_order");

            entity.HasOne(d => d.ProductUnit).WithMany(p => p.PurchaseOrderLines)
                .HasPrincipalKey(p => new { p.Id, p.ProductId })
                .HasForeignKey(d => new { d.UnitId, d.ProductId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_purchase_order_lines_unit");
        });

        modelBuilder.Entity<ReceiptLine>(entity =>
        {
            entity.ToTable("receipt_lines", "purchasing");

            entity.HasIndex(e => e.ProductId, "IX_receipt_lines_product");

            entity.HasIndex(e => new { e.ReceiptId, e.LineNo }, "UQ_receipt_lines_line_no").IsUnique();

            entity.HasIndex(e => e.LotId, "UX_receipt_lines_lot")
                .IsUnique()
                .HasFilter("([lot_id] IS NOT NULL)");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BaseQty).HasColumnName("base_qty");
            entity.Property(e => e.ExpiryDate).HasColumnName("expiry_date");
            entity.Property(e => e.LineNo).HasColumnName("line_no");
            entity.Property(e => e.LineTotal).HasColumnName("line_total");
            entity.Property(e => e.LotId).HasColumnName("lot_id");
            entity.Property(e => e.LotNo)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("lot_no");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Qty).HasColumnName("qty");
            entity.Property(e => e.ReceiptId).HasColumnName("receipt_id");
            entity.Property(e => e.UnitId).HasColumnName("unit_id");
            entity.Property(e => e.UnitPrice).HasColumnName("unit_price");

            entity.HasOne(d => d.Receipt).WithMany(p => p.ReceiptLines)
                .HasForeignKey(d => d.ReceiptId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_receipt_lines_receipt");

            entity.HasOne(d => d.Lot).WithMany(p => p.ReceiptLines)
                .HasPrincipalKey(p => new { p.Id, p.ProductId })
                .HasForeignKey(d => new { d.LotId, d.ProductId })
                .HasConstraintName("FK_receipt_lines_lot");

            entity.HasOne(d => d.ProductUnit).WithMany(p => p.ReceiptLines)
                .HasPrincipalKey(p => new { p.Id, p.ProductId })
                .HasForeignKey(d => new { d.UnitId, d.ProductId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_receipt_lines_unit");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens", "auth");

            entity.HasIndex(e => e.UserId, "IX_refresh_tokens_user");

            entity.HasIndex(e => e.TokenHash, "UQ_refresh_tokens_token_hash").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_refresh_tokens_created_at")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt)
                .HasPrecision(3)
                .HasColumnName("expires_at");
            entity.Property(e => e.ReplacedById).HasColumnName("replaced_by_id");
            entity.Property(e => e.RevokedAt)
                .HasPrecision(3)
                .HasColumnName("revoked_at");
            entity.Property(e => e.TokenHash)
                .HasMaxLength(64)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("token_hash");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.ReplacedBy).WithMany(p => p.InverseReplacedBy)
                .HasForeignKey(d => d.ReplacedById)
                .HasConstraintName("FK_refresh_tokens_replaced_by");

            entity.HasOne(d => d.User).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_refresh_tokens_user");
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.HasKey(e => e.Id).IsClustered(false);

            entity.ToTable("sales", "sales");

            entity.HasIndex(e => e.Seq, "CX_sales_seq")
                .IsUnique()
                .IsClustered();

            entity.HasIndex(e => new { e.CreatedAt, e.Status }, "IX_sales_created_status");

            entity.HasIndex(e => e.CreatedAt, "IX_sales_pending").HasFilter("([status]='pending_payment')");

            entity.HasIndex(e => e.Code, "UQ_sales_code").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CancelReason)
                .HasMaxLength(300)
                .HasColumnName("cancel_reason");
            entity.Property(e => e.CancelledAt)
                .HasPrecision(3)
                .HasColumnName("cancelled_at");
            entity.Property(e => e.CancelledBy).HasColumnName("cancelled_by");
            entity.Property(e => e.CashGiven).HasColumnName("cash_given");
            entity.Property(e => e.Code)
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasDefaultValueSql("('HD'+right('00000000'+CONVERT([varchar](10),NEXT VALUE FOR [sales].[seq_sale_code]),(8)))", "DF_sales_code")
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.IsOffline).HasColumnName("is_offline");
            entity.Property(e => e.LocationId).HasColumnName("location_id");
            entity.Property(e => e.PaidAt)
                .HasPrecision(3)
                .HasColumnName("paid_at");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("payment_method");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken()
                .HasColumnName("row_version");
            entity.Property(e => e.Seq)
                .ValueGeneratedOnAdd()
                .HasColumnName("seq");
            entity.Property(e => e.Status)
                .HasMaxLength(16)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.SyncedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_sales_synced_at")
                .HasColumnName("synced_at");
            entity.Property(e => e.Total).HasColumnName("total");

            entity.HasOne(d => d.CancelledByNavigation).WithMany(p => p.SaleCancelledByNavigations)
                .HasForeignKey(d => d.CancelledBy)
                .HasConstraintName("FK_sales_cancelled_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.SaleCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_sales_created_by");

            entity.HasOne(d => d.Location).WithMany(p => p.Sales)
                .HasForeignKey(d => d.LocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_sales_location");
        });

        modelBuilder.Entity<SaleLine>(entity =>
        {
            entity.ToTable("sale_lines", "sales");

            entity.HasIndex(e => e.ProductId, "IX_sale_lines_product").HasFilter("([product_id] IS NOT NULL)");

            entity.HasIndex(e => e.SaleId, "IX_sale_lines_sale");

            entity.HasIndex(e => e.SaleId, "IX_sale_lines_undeclared").HasFilter("([product_id] IS NULL)");

            entity.HasIndex(e => new { e.SaleId, e.LineNo }, "UQ_sale_lines_line_no").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BaseQty).HasColumnName("base_qty");
            entity.Property(e => e.CostTotal)
                .HasColumnType("decimal(18, 4)")
                .HasColumnName("cost_total");
            entity.Property(e => e.ExpiredConfirmed).HasColumnName("expired_confirmed");
            entity.Property(e => e.LineNo).HasColumnName("line_no");
            entity.Property(e => e.LineTotal).HasColumnName("line_total");
            entity.Property(e => e.NameSnapshot)
                .HasMaxLength(200)
                .HasColumnName("name_snapshot");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Qty).HasColumnName("qty");
            entity.Property(e => e.SaleId).HasColumnName("sale_id");
            entity.Property(e => e.UnitId).HasColumnName("unit_id");
            entity.Property(e => e.UnitPrice).HasColumnName("unit_price");

            entity.HasOne(d => d.Sale).WithMany(p => p.SaleLines)
                .HasForeignKey(d => d.SaleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_sale_lines_sale");

            entity.HasOne(d => d.ProductUnit).WithMany(p => p.SaleLines)
                .HasPrincipalKey(p => new { p.Id, p.ProductId })
                .HasForeignKey(d => new { d.UnitId, d.ProductId })
                .HasConstraintName("FK_sale_lines_unit");
        });

        modelBuilder.Entity<Stock>(entity =>
        {
            entity.HasKey(e => new { e.LotId, e.LocationId });

            entity.ToTable("stock", "inventory");

            entity.HasIndex(e => new { e.ProductId, e.LocationId }, "IX_stock_product_location");

            entity.Property(e => e.LotId).HasColumnName("lot_id");
            entity.Property(e => e.LocationId).HasColumnName("location_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Qty).HasColumnName("qty");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken()
                .HasColumnName("row_version");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_stock_updated_at")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Location).WithMany(p => p.Stocks)
                .HasForeignKey(d => d.LocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_stock_location");

            entity.HasOne(d => d.Lot).WithMany(p => p.Stocks)
                .HasPrincipalKey(p => new { p.Id, p.ProductId })
                .HasForeignKey(d => new { d.LotId, d.ProductId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_stock_lot");
        });

        modelBuilder.Entity<StockCount>(entity =>
        {
            entity.ToTable("stock_counts", "inventory");

            entity.HasIndex(e => new { e.Status, e.CreatedAt }, "IX_stock_counts_status");

            entity.HasIndex(e => e.Code, "UQ_stock_counts_code").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ApprovedAt)
                .HasPrecision(3)
                .HasColumnName("approved_at");
            entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.Code)
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasDefaultValueSql("('KK'+right('00000000'+CONVERT([varchar](10),NEXT VALUE FOR [inventory].[seq_stock_count_code]),(8)))", "DF_stock_counts_code")
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_stock_counts_created_at")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.LocationId).HasColumnName("location_id");
            entity.Property(e => e.Note)
                .HasMaxLength(500)
                .HasColumnName("note");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken()
                .HasColumnName("row_version");
            entity.Property(e => e.ScopeType)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("scope_type");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("counting", "DF_stock_counts_status")
                .HasColumnName("status");
            entity.Property(e => e.SubmittedAt)
                .HasPrecision(3)
                .HasColumnName("submitted_at");
            entity.Property(e => e.SubmittedBy).HasColumnName("submitted_by");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.StockCountApprovedByNavigations)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("FK_stock_counts_approved_by");

            entity.HasOne(d => d.Category).WithMany(p => p.StockCounts)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK_stock_counts_category");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.StockCountCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_stock_counts_created_by");

            entity.HasOne(d => d.Location).WithMany(p => p.StockCounts)
                .HasForeignKey(d => d.LocationId)
                .HasConstraintName("FK_stock_counts_location");

            entity.HasOne(d => d.SubmittedByNavigation).WithMany(p => p.StockCountSubmittedByNavigations)
                .HasForeignKey(d => d.SubmittedBy)
                .HasConstraintName("FK_stock_counts_submitted_by");
        });

        modelBuilder.Entity<StockMove>(entity =>
        {
            entity.ToTable("stock_moves", "inventory", tb =>
                {
                    tb.HasTrigger("TR_stock_moves_append_only");
                    tb.HasTrigger("TR_stock_moves_apply");
                });

            entity.HasIndex(e => e.CountLineId, "IX_stock_moves_count_line").HasFilter("([count_line_id] IS NOT NULL)");

            entity.HasIndex(e => e.DisposalLineId, "IX_stock_moves_disposal_line").HasFilter("([disposal_line_id] IS NOT NULL)");

            entity.HasIndex(e => new { e.LotId, e.LocationId }, "IX_stock_moves_lot_location");

            entity.HasIndex(e => new { e.ProductId, e.CreatedAt }, "IX_stock_moves_product_created");

            entity.HasIndex(e => e.ReceiptLineId, "IX_stock_moves_receipt_line").HasFilter("([receipt_line_id] IS NOT NULL)");

            entity.HasIndex(e => e.SaleLineId, "IX_stock_moves_sale_line").HasFilter("([sale_line_id] IS NOT NULL)");

            entity.HasIndex(e => e.TransferLineId, "IX_stock_moves_transfer_line").HasFilter("([transfer_line_id] IS NOT NULL)");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CountLineId).HasColumnName("count_line_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_stock_moves_created_at")
                .HasColumnName("created_at");
            entity.Property(e => e.DisposalLineId).HasColumnName("disposal_line_id");
            entity.Property(e => e.DocType)
                .HasMaxLength(16)
                .IsUnicode(false)
                .HasColumnName("doc_type");
            entity.Property(e => e.LocationId).HasColumnName("location_id");
            entity.Property(e => e.LotId).HasColumnName("lot_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.QtyChange).HasColumnName("qty_change");
            entity.Property(e => e.ReceiptLineId).HasColumnName("receipt_line_id");
            entity.Property(e => e.SaleLineId).HasColumnName("sale_line_id");
            entity.Property(e => e.TransferLineId).HasColumnName("transfer_line_id");
            entity.Property(e => e.UnitCost)
                .HasColumnType("decimal(14, 4)")
                .HasColumnName("unit_cost");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.CountLine).WithMany(p => p.StockMoves)
                .HasForeignKey(d => d.CountLineId)
                .HasConstraintName("FK_stock_moves_count_line");

            entity.HasOne(d => d.DisposalLine).WithMany(p => p.StockMoves)
                .HasForeignKey(d => d.DisposalLineId)
                .HasConstraintName("FK_stock_moves_disposal_line");

            entity.HasOne(d => d.Location).WithMany(p => p.StockMoves)
                .HasForeignKey(d => d.LocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_stock_moves_location");

            entity.HasOne(d => d.ReceiptLine).WithMany(p => p.StockMoves)
                .HasForeignKey(d => d.ReceiptLineId)
                .HasConstraintName("FK_stock_moves_receipt_line");

            entity.HasOne(d => d.SaleLine).WithMany(p => p.StockMoves)
                .HasForeignKey(d => d.SaleLineId)
                .HasConstraintName("FK_stock_moves_sale_line");

            entity.HasOne(d => d.TransferLine).WithMany(p => p.StockMoves)
                .HasForeignKey(d => d.TransferLineId)
                .HasConstraintName("FK_stock_moves_transfer_line");

            entity.HasOne(d => d.User).WithMany(p => p.StockMoves)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_stock_moves_user");

            entity.HasOne(d => d.Lot).WithMany(p => p.StockMoves)
                .HasPrincipalKey(p => new { p.Id, p.ProductId })
                .HasForeignKey(d => new { d.LotId, d.ProductId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_stock_moves_lot");
        });

        modelBuilder.Entity<Store>(entity =>
        {
            entity.ToTable("stores", "core");

            entity.Property(e => e.Id)
                .HasDefaultValue((byte)1, "DF_stores_id")
                .HasColumnName("id");
            entity.Property(e => e.Address)
                .HasMaxLength(300)
                .HasColumnName("address");
            entity.Property(e => e.BankAccountName)
                .HasMaxLength(100)
                .HasColumnName("bank_account_name");
            entity.Property(e => e.BankAccountNo)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("bank_account_no");
            entity.Property(e => e.BankBin)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("bank_bin");
            entity.Property(e => e.CountIntervalDays)
                .HasDefaultValue((short)30, "DF_stores_count_interval_days")
                .HasColumnName("count_interval_days");
            entity.Property(e => e.ExpiryWarnDays)
                .HasDefaultValue((short)7, "DF_stores_expiry_warn_days")
                .HasColumnName("expiry_warn_days");
            entity.Property(e => e.IdleLockMinutes)
                .HasDefaultValue((short)15, "DF_stores_idle_lock_minutes")
                .HasColumnName("idle_lock_minutes");
            entity.Property(e => e.Name)
                .HasMaxLength(150)
                .HasColumnName("name");
            entity.Property(e => e.Phone)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("phone");
            entity.Property(e => e.ReorderCoverDays)
                .HasDefaultValue((short)7, "DF_stores_reorder_cover_days")
                .HasColumnName("reorder_cover_days");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken()
                .HasColumnName("row_version");
            entity.Property(e => e.SalesLookbackDays)
                .HasDefaultValue((short)14, "DF_stores_sales_lookback_days")
                .HasColumnName("sales_lookback_days");
            entity.Property(e => e.StaffCanSeeCost).HasColumnName("staff_can_see_cost");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.Stores)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("FK_stores_updated_by");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.ToTable("suppliers", "catalog");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_suppliers_created_at")
                .HasColumnName("created_at");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true, "DF_suppliers_is_active")
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(150)
                .HasColumnName("name");
            entity.Property(e => e.Note)
                .HasMaxLength(500)
                .HasColumnName("note");
            entity.Property(e => e.Phone)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("phone");
            entity.Property(e => e.Zalo)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("zalo");
        });

        modelBuilder.Entity<Transfer>(entity =>
        {
            entity.ToTable("transfers", "inventory");

            entity.HasIndex(e => e.CreatedAt, "IX_transfers_created");

            entity.HasIndex(e => e.Code, "UQ_transfers_code").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasDefaultValueSql("('CK'+right('00000000'+CONVERT([varchar](10),NEXT VALUE FOR [inventory].[seq_transfer_code]),(8)))", "DF_transfers_code")
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_transfers_created_at")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.FromLocationId).HasColumnName("from_location_id");
            entity.Property(e => e.IsAuto).HasColumnName("is_auto");
            entity.Property(e => e.Note)
                .HasMaxLength(500)
                .HasColumnName("note");
            entity.Property(e => e.SaleId).HasColumnName("sale_id");
            entity.Property(e => e.ToLocationId).HasColumnName("to_location_id");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Transfers)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_transfers_created_by");

            entity.HasOne(d => d.FromLocation).WithMany(p => p.TransferFromLocations)
                .HasForeignKey(d => d.FromLocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_transfers_from");

            entity.HasOne(d => d.Sale).WithMany(p => p.Transfers)
                .HasForeignKey(d => d.SaleId)
                .HasConstraintName("FK_transfers_sale");

            entity.HasOne(d => d.ToLocation).WithMany(p => p.TransferToLocations)
                .HasForeignKey(d => d.ToLocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_transfers_to");
        });

        modelBuilder.Entity<TransferLine>(entity =>
        {
            entity.ToTable("transfer_lines", "inventory");

            entity.HasIndex(e => new { e.TransferId, e.ProductId }, "UQ_transfer_lines_product").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BaseQty).HasColumnName("base_qty");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.TransferId).HasColumnName("transfer_id");

            entity.HasOne(d => d.Product).WithMany(p => p.TransferLines)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_transfer_lines_product");

            entity.HasOne(d => d.Transfer).WithMany(p => p.TransferLines)
                .HasForeignKey(d => d.TransferId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_transfer_lines_transfer");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users", "auth");

            entity.HasIndex(e => e.Phone, "UQ_users_phone").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_users_created_at")
                .HasColumnName("created_at");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true, "DF_users_is_active")
                .HasColumnName("is_active");
            entity.Property(e => e.LastLoginAt)
                .HasPrecision(3)
                .HasColumnName("last_login_at");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("password_hash");
            entity.Property(e => e.Phone)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("phone");
            entity.Property(e => e.PinHash)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("pin_hash");
            entity.Property(e => e.Role)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("role");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken()
                .HasColumnName("row_version");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<VProductStock>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_product_stock", "inventory");

            entity.Property(e => e.LocationId).HasColumnName("location_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Qty).HasColumnName("qty");
        });

        modelBuilder.Entity<VStockLedgerMismatch>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_stock_ledger_mismatch", "inventory");

            entity.Property(e => e.LedgerQty).HasColumnName("ledger_qty");
            entity.Property(e => e.LocationId).HasColumnName("location_id");
            entity.Property(e => e.LotId).HasColumnName("lot_id");
            entity.Property(e => e.StockQty).HasColumnName("stock_qty");
        });
        modelBuilder.HasSequence<int>("seq_disposal_code", "inventory");
        modelBuilder.HasSequence<int>("seq_goods_receipt_code", "purchasing");
        modelBuilder.HasSequence<int>("seq_purchase_order_code", "purchasing");
        modelBuilder.HasSequence<int>("seq_sale_code", "sales");
        modelBuilder.HasSequence<int>("seq_stock_count_code", "inventory");
        modelBuilder.HasSequence<int>("seq_transfer_code", "inventory");

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
