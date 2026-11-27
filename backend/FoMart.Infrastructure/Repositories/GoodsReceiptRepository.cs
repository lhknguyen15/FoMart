using FoMart.Application.DTO;
using FoMart.Application.DTO.Purchasing;
using FoMart.Application.Interfaces.Repositories;
using FoMart.Domain.Entities;
using FoMart.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FoMart.Infrastructure.Repositories;

public sealed class GoodsReceiptRepository(FoMartDbContext dbContext)
    : RepositoryBase<GoodsReceipt>(dbContext), IGoodsReceiptRepository
{
    // Tìm phiếu nhập, mới nhất trước.
    public async Task<PagedResult<GoodsReceiptListItemResponse>> SearchAsync(
        GoodsReceiptSearchRequest request, DateTime? createdFromUtc, DateTime? createdToUtc, CancellationToken cancellationToken = default)
    {
        var query = DbContext.GoodsReceipts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(r => r.Code.Contains(keyword));
        }
        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(r => r.Status == request.Status);
        if (request.SupplierId is { } supplierId)
            query = query.Where(r => r.SupplierId == supplierId);
        if (createdFromUtc is { } from)
            query = query.Where(r => r.CreatedAt >= from);
        if (createdToUtc is { } to)
            query = query.Where(r => r.CreatedAt < to);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new GoodsReceiptListItemResponse(
                r.Id,
                r.Code,
                r.Supplier != null ? r.Supplier.Name : null,
                r.Location.Name,
                r.Status,
                r.Total,
                r.ReceiptLines.Count,
                r.CreatedByNavigation.Name,
                r.CreatedAt,
                r.CompletedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<GoodsReceiptListItemResponse>(items, request.Page, request.PageSize, total);
    }

    // Phiếu được theo dõi kèm mọi thứ cần để sửa, hoàn tất, hủy và dựng phản hồi.
    public Task<GoodsReceipt?> GetWithLinesAsync(int receiptId, CancellationToken cancellationToken = default) =>
        DbContext.GoodsReceipts
            .Include(r => r.ReceiptLines).ThenInclude(l => l.ProductUnit).ThenInclude(u => u.Product)
            .Include(r => r.ReceiptLines).ThenInclude(l => l.Lot)
            .Include(r => r.Supplier)
            .Include(r => r.Location)
            .Include(r => r.CreatedByNavigation)
            .Include(r => r.CompletedByNavigation)
            .Include(r => r.CancelledByNavigation)
            .AsSplitQuery()
            .SingleOrDefaultAsync(r => r.Id == receiptId, cancellationToken);

    // Xóa dòng của phiếu nháp.
    public void RemoveLine(ReceiptLine line) => DbContext.ReceiptLines.Remove(line);

    // Giá nhập gần nhất của đơn vị trên phiếu đã hoàn tất (bỏ phiếu đã hủy và dòng chưa có giá).
    public Task<LastPurchasePriceResponse?> GetLastPriceAsync(int unitId, CancellationToken cancellationToken = default) =>
        DbContext.ReceiptLines.AsNoTracking()
            .Where(l => l.UnitId == unitId && l.UnitPrice != null && l.Receipt.Status == GoodsReceiptStatus.Completed)
            .OrderByDescending(l => l.Receipt.CompletedAt).ThenByDescending(l => l.Id)
            .Select(l => new LastPurchasePriceResponse(
                l.UnitId,
                l.UnitPrice!.Value,
                l.Receipt.CompletedAt!.Value,
                l.Receipt.Code,
                l.Receipt.Supplier != null ? l.Receipt.Supplier.Name : null))
            .FirstOrDefaultAsync(cancellationToken);
}
