using FoMart.Application.DTO;
using FoMart.Application.DTO.Inventory;
using FoMart.Application.Interfaces.Repositories;
using FoMart.Domain.Entities;
using FoMart.Domain.Enums;
using FoMart.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace FoMart.Infrastructure.Repositories;

// Lõi kho. Chỉ THÊM lots và stock_moves; bảng stock do trigger TR_stock_moves_apply duy trì (BR-02).
public sealed class StockRepository(FoMartDbContext dbContext)
    : RepositoryBase<StockMove>(dbContext), IStockRepository
{
    // Thêm lô mới.
    public Task AddLotAsync(Lot lot, CancellationToken cancellationToken = default) =>
        DbContext.Lots.AddAsync(lot, cancellationToken).AsTask();

    // Khóa dòng tồn của các lô (UPDLOCK, ROWLOCK) theo lot_id tăng dần để hai nghiệp vụ không khóa chéo nhau.
    // Khóa được giữ tới khi transaction của unitOfWork.BeginWriteAsync kết thúc.
    public async Task LockLotsAsync(IReadOnlyCollection<int> lotIds, CancellationToken cancellationToken = default)
    {
        if (lotIds.Count == 0) return;
        EnsureTransaction();

        var csv = ToCsv(lotIds);
        await DbContext.Database
            .SqlQuery<int>($"""
                SELECT s.lot_id AS [Value]
                FROM inventory.stock AS s WITH (UPDLOCK, ROWLOCK)
                WHERE s.lot_id IN (SELECT CAST(value AS INT) FROM STRING_SPLIT({csv}, ','))
                ORDER BY s.lot_id
                """)
            .ToListAsync(cancellationToken);
    }

    // Khóa dòng sản phẩm theo id tăng dần. Khóa cả khi vị trí chưa có dòng tồn nào,
    // nên hai lượt bán đầu tiên cùng sản phẩm không cùng tạo lô tạm.
    public async Task LockProductsAsync(IReadOnlyCollection<int> productIds, CancellationToken cancellationToken = default)
    {
        if (productIds.Count == 0) return;
        EnsureTransaction();

        var csv = ToCsv(productIds);
        await DbContext.Database
            .SqlQuery<int>($"""
                SELECT p.id AS [Value]
                FROM catalog.products AS p WITH (UPDLOCK, ROWLOCK)
                WHERE p.id IN (SELECT CAST(value AS INT) FROM STRING_SPLIT({csv}, ','))
                ORDER BY p.id
                """)
            .ToListAsync(cancellationToken);
    }

    // Tồn theo lô tại vị trí (khóa dòng tồn), kèm loại lô, hạn dùng, ngày nhận, giá vốn để chia FEFO.
    public async Task<IReadOnlyDictionary<int, IReadOnlyList<LotStock>>> GetLotStocksAsync(
        IReadOnlyCollection<int> productIds, int locationId, CancellationToken cancellationToken = default)
    {
        if (productIds.Count == 0) return new Dictionary<int, IReadOnlyList<LotStock>>();
        EnsureTransaction();

        var csv = ToCsv(productIds);
        var rows = await DbContext.Database
            .SqlQuery<LotStockRow>($"""
                SELECT s.product_id AS ProductId, s.lot_id AS LotId, l.kind AS Kind, l.expiry_date AS ExpiryDate,
                       l.received_at AS ReceivedAt, l.unit_cost AS UnitCost, s.qty AS Qty
                FROM inventory.stock AS s WITH (UPDLOCK, ROWLOCK)
                JOIN inventory.lots AS l ON l.id = s.lot_id
                WHERE s.location_id = {locationId}
                  AND s.product_id IN (SELECT CAST(value AS INT) FROM STRING_SPLIT({csv}, ','))
                ORDER BY s.product_id, s.lot_id
                """)
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(r => r.ProductId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<LotStock>)[.. g.Select(r => new LotStock(r.LotId, r.Kind, r.ExpiryDate, r.ReceivedAt, r.UnitCost, r.Qty))]);
    }

    // Lô tạm của sản phẩm.
    public Task<int?> GetProvisionalLotIdAsync(int productId, CancellationToken cancellationToken = default) =>
        DbContext.Lots.AsNoTracking()
            .Where(l => l.ProductId == productId && l.Kind == LotKind.Provisional)
            .Select(l => (int?)l.Id)
            .SingleOrDefaultAsync(cancellationToken);

    // Bật needs_count; chạy thẳng bằng UPDATE trong transaction hiện tại.
    public async Task MarkNeedsCountAsync(IReadOnlyCollection<int> productIds, CancellationToken cancellationToken = default)
    {
        if (productIds.Count == 0) return;
        await DbContext.Products
            .Where(p => productIds.Contains(p.Id) && !p.NeedsCount)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.NeedsCount, true), cancellationToken);
    }

    // Dòng sổ của hóa đơn: nối qua sale_lines.
    public async Task<IReadOnlyList<StockMove>> GetSaleMovesAsync(Guid saleId, CancellationToken cancellationToken = default) =>
        await DbContext.StockMoves.AsNoTracking()
            .Include(m => m.Lot)
            .Where(m => m.SaleLine != null && m.SaleLine.SaleId == saleId)
            .OrderBy(m => m.Id)
            .ToListAsync(cancellationToken);

    // Lô nào đã có biến động khác loại chứng từ exceptDocType.
    public async Task<IReadOnlyList<int>> GetLotsMovedByOtherDocsAsync(
        IReadOnlyCollection<int> lotIds, string exceptDocType, CancellationToken cancellationToken = default)
    {
        if (lotIds.Count == 0) return [];
        return await DbContext.StockMoves.AsNoTracking()
            .Where(m => lotIds.Contains(m.LotId) && m.DocType != exceptDocType)
            .Select(m => m.LotId)
            .Distinct()
            .OrderBy(id => id)
            .ToListAsync(cancellationToken);
    }

    // Tồn theo lô và vị trí: sắp theo sản phẩm, rồi FEFO (hạn dùng gần nhất, lô không hạn để sau), rồi FIFO.
    public async Task<PagedResult<StockLotResponse>> SearchStockAsync(
        StockSearchRequest request, DateOnly? expiryFrom, DateOnly? expiryBefore, CancellationToken cancellationToken = default)
    {
        var query = DbContext.Stocks.AsNoTracking();
        if (request.ProductId is { } productId)
            query = query.Where(s => s.ProductId == productId);
        if (request.LocationId is { } locationId)
            query = query.Where(s => s.LocationId == locationId);
        if (!request.IncludeZero)
            query = query.Where(s => s.Qty != 0);
        if (expiryBefore is { } before)
        {
            query = query.Where(s => s.Qty > 0 && s.Lot.ExpiryDate != null && s.Lot.ExpiryDate < before);
            if (expiryFrom is { } from)
                query = query.Where(s => s.Lot.ExpiryDate >= from);
        }

        var total = await query.CountAsync(cancellationToken);
        // Màn hình hạn dùng: lô gấp nhất trước; còn lại: theo sản phẩm, trong sản phẩm theo FEFO.
        var ordered = expiryBefore is not null
            ? query.OrderBy(s => s.Lot.ExpiryDate).ThenBy(s => s.Lot.Product.Name).ThenBy(s => s.LotId).ThenBy(s => s.LocationId)
            : query.OrderBy(s => s.Lot.Product.Name).ThenBy(s => s.ProductId)
                .ThenBy(s => s.Lot.ExpiryDate == null).ThenBy(s => s.Lot.ExpiryDate)
                .ThenBy(s => s.Lot.ReceivedAt).ThenBy(s => s.LotId)
                .ThenBy(s => s.LocationId);
        var items = await ordered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new StockLotResponse(
                s.ProductId,
                s.Lot.Product.Name,
                s.Lot.Product.ProductUnits.Where(u => u.IsBase).Select(u => u.Name).FirstOrDefault() ?? "",
                s.LotId,
                s.Lot.Kind,
                s.Lot.LotNo,
                s.Lot.ExpiryDate,
                s.Lot.ReceivedAt,
                s.Lot.UnitCost,
                s.LocationId,
                s.Location.Name,
                s.Qty))
            .ToListAsync(cancellationToken);

        return new PagedResult<StockLotResponse>(items, request.Page, request.PageSize, total);
    }

    // Lô có hạn dùng trước nearBefore và còn tồn: tiệm nhỏ chỉ có vài chục dòng, nên lấy về rồi cộng ở bộ nhớ.
    public async Task<StockExpirySummaryResponse> GetExpirySummaryAsync(
        DateOnly today, DateOnly nearBefore, int warnDays, CancellationToken cancellationToken = default)
    {
        var rows = await DbContext.Stocks.AsNoTracking()
            .Where(s => s.Qty > 0 && s.Lot.ExpiryDate != null && s.Lot.ExpiryDate < nearBefore)
            .Select(s => new { Expiry = s.Lot.ExpiryDate!.Value, s.Qty, s.Lot.UnitCost })
            .ToListAsync(cancellationToken);

        var expired = rows.Where(r => r.Expiry < today).ToList();
        var near = rows.Where(r => r.Expiry >= today).ToList();
        // Giá trị theo giá vốn chính xác, làm tròn một lần về đồng (BR-11).
        return new StockExpirySummaryResponse(
            warnDays,
            expired.Count,
            expired.Sum(r => r.Qty),
            MoneyRules.ToDong(expired.Sum(r => r.Qty * (r.UnitCost ?? 0m))),
            near.Count,
            near.Sum(r => r.Qty),
            MoneyRules.ToDong(near.Sum(r => r.Qty * (r.UnitCost ?? 0m))));
    }

    // Thẻ kho, mới nhất trước. Balance tính bằng tổng các biến động có id ≤ id dòng hiện tại
    // (cùng sản phẩm, cùng vị trí nếu có lọc), nên đúng cả khi lọc theo khoảng ngày.
    public async Task<PagedResult<StockMoveResponse>> GetMovesAsync(
        StockMoveSearchRequest request, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default)
    {
        var locationId = request.LocationId;
        var moves = DbContext.StockMoves.AsNoTracking().Where(m => m.ProductId == request.ProductId);
        if (locationId is not null)
            moves = moves.Where(m => m.LocationId == locationId);

        var query = moves;
        if (fromUtc is { } from)
            query = query.Where(m => m.CreatedAt >= from);
        if (toUtc is { } to)
            query = query.Where(m => m.CreatedAt < to);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(m => m.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(m => new StockMoveResponse(
                m.Id,
                m.CreatedAt,
                m.DocType,
                m.ReceiptLineId != null ? m.ReceiptLine!.Receipt.Code
                    : m.SaleLineId != null ? m.SaleLine!.Sale.Code
                    : m.TransferLineId != null ? m.TransferLine!.Transfer.Code
                    : m.DisposalLineId != null ? m.DisposalLine!.Disposal.Code
                    : m.CountLine!.Count.Code,
                m.LotId,
                m.Lot.LotNo,
                m.Lot.ExpiryDate,
                m.LocationId,
                m.Location.Name,
                m.QtyChange,
                moves.Where(x => x.Id <= m.Id).Sum(x => x.QtyChange),
                m.UnitCost,
                m.User.Name,
                m.SaleLineId != null ? m.SaleLine!.SaleId : null,
                m.ReceiptLineId != null ? m.ReceiptLine!.ReceiptId : null))
            .ToListAsync(cancellationToken);

        return new PagedResult<StockMoveResponse>(items, request.Page, request.PageSize, total);
    }

    // Khóa dòng chỉ có tác dụng khi giữ tới hết transaction của unitOfWork.BeginWriteAsync.
    private void EnsureTransaction()
    {
        if (DbContext.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Stock locks must run inside unitOfWork.BeginWriteAsync.");
    }

    // Danh sách id không trùng, tăng dần, nối bằng dấu phẩy cho STRING_SPLIT.
    private static string ToCsv(IEnumerable<int> ids) => string.Join(',', ids.Distinct().Order());

    // View v_stock_ledger_mismatch.
    public async Task<IReadOnlyList<StockLedgerMismatchResponse>> GetLedgerMismatchesAsync(CancellationToken cancellationToken = default) =>
        await DbContext.VStockLedgerMismatches.AsNoTracking()
            .Select(v => new StockLedgerMismatchResponse(v.LotId, v.LocationId, v.StockQty, v.LedgerQty))
            .ToListAsync(cancellationToken);
}

// Một dòng kết quả SQL của GetLotStocksAsync (EF đọc theo tên cột), đổi sang LotStock của Domain.
internal sealed class LotStockRow
{
    public int ProductId { get; set; }

    public int LotId { get; set; }

    public string Kind { get; set; } = "";

    public DateOnly? ExpiryDate { get; set; }

    public DateTime ReceivedAt { get; set; }

    public decimal? UnitCost { get; set; }

    public int Qty { get; set; }
}
