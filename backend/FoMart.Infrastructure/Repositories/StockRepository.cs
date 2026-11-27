using FoMart.Application.DTO;
using FoMart.Application.DTO.Inventory;
using FoMart.Application.Interfaces.Repositories;
using FoMart.Domain.Entities;
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
        if (DbContext.Database.CurrentTransaction is null)
            throw new InvalidOperationException("LockLotsAsync must run inside unitOfWork.BeginWriteAsync.");

        var csv = string.Join(',', lotIds.Distinct().Order());
        await DbContext.Database
            .SqlQuery<int>($"""
                SELECT s.lot_id AS [Value]
                FROM inventory.stock AS s WITH (UPDLOCK, ROWLOCK)
                WHERE s.lot_id IN (SELECT CAST(value AS INT) FROM STRING_SPLIT({csv}, ','))
                ORDER BY s.lot_id
                """)
            .ToListAsync(cancellationToken);
    }

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
    public async Task<PagedResult<StockLotResponse>> SearchStockAsync(StockSearchRequest request, CancellationToken cancellationToken = default)
    {
        var query = DbContext.Stocks.AsNoTracking();
        if (request.ProductId is { } productId)
            query = query.Where(s => s.ProductId == productId);
        if (request.LocationId is { } locationId)
            query = query.Where(s => s.LocationId == locationId);
        if (!request.IncludeZero)
            query = query.Where(s => s.Qty != 0);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(s => s.Lot.Product.Name).ThenBy(s => s.ProductId)
            .ThenBy(s => s.Lot.ExpiryDate == null).ThenBy(s => s.Lot.ExpiryDate)
            .ThenBy(s => s.Lot.ReceivedAt).ThenBy(s => s.LotId)
            .ThenBy(s => s.LocationId)
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
                m.User.Name))
            .ToListAsync(cancellationToken);

        return new PagedResult<StockMoveResponse>(items, request.Page, request.PageSize, total);
    }

    // View v_stock_ledger_mismatch.
    public async Task<IReadOnlyList<StockLedgerMismatchResponse>> GetLedgerMismatchesAsync(CancellationToken cancellationToken = default) =>
        await DbContext.VStockLedgerMismatches.AsNoTracking()
            .Select(v => new StockLedgerMismatchResponse(v.LotId, v.LocationId, v.StockQty, v.LedgerQty))
            .ToListAsync(cancellationToken);
}
