using FoMart.Application.DTO;
using FoMart.Application.DTO.Inventory;
using FoMart.Application.Interfaces.Repositories;
using FoMart.Domain.Entities;
using FoMart.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FoMart.Infrastructure.Repositories;

public sealed class DisposalRepository(FoMartDbContext dbContext)
    : RepositoryBase<Disposal>(dbContext), IDisposalRepository
{
    // Tìm phiếu hủy, mới nhất trước. Tổn thất cộng từ sổ kho (chỉ có sau khi duyệt), làm tròn về đồng (BR-11).
    public async Task<PagedResult<DisposalListItemResponse>> SearchAsync(DisposalSearchRequest request, CancellationToken cancellationToken = default)
    {
        var query = DbContext.Disposals.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(d => d.Status == request.Status);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(d => d.CreatedAt).ThenByDescending(d => d.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(d => new DisposalListItemResponse(
                d.Id,
                d.Code,
                d.Status,
                d.DisposalLines.Count,
                d.DisposalLines.Sum(l => l.BaseQty),
                d.Status == DisposalStatus.Approved
                    ? (long?)Math.Round(DbContext.StockMoves
                        .Where(m => m.DisposalLine != null && m.DisposalLine.DisposalId == d.Id)
                        .Sum(m => -m.QtyChange * (m.UnitCost ?? 0m)), 0)
                    : null,
                d.CreatedByNavigation.Name,
                d.CreatedAt,
                d.DecidedByNavigation != null ? d.DecidedByNavigation.Name : null,
                d.DecidedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<DisposalListItemResponse>(items, request.Page, request.PageSize, total);
    }

    // Phiếu được theo dõi kèm mọi thứ cần để trả chi tiết và để duyệt.
    public Task<Disposal?> GetWithLinesAsync(int disposalId, CancellationToken cancellationToken = default) =>
        DbContext.Disposals
            .Include(d => d.DisposalLines).ThenInclude(l => l.Product).ThenInclude(p => p.ProductUnits)
            .Include(d => d.DisposalLines).ThenInclude(l => l.Location)
            .Include(d => d.DisposalLines).ThenInclude(l => l.Lot)
            .Include(d => d.CreatedByNavigation)
            .Include(d => d.DecidedByNavigation)
            .AsSplitQuery()
            .SingleOrDefaultAsync(d => d.Id == disposalId, cancellationToken);

    // Sổ kho của phiếu, theo id tăng dần.
    public async Task<IReadOnlyList<StockMove>> GetMovesAsync(int disposalId, CancellationToken cancellationToken = default) =>
        await DbContext.StockMoves.AsNoTracking()
            .Include(m => m.Lot)
            .Where(m => m.DisposalLine != null && m.DisposalLine.DisposalId == disposalId)
            .OrderBy(m => m.Id)
            .ToListAsync(cancellationToken);
}
