using FoMart.Application.DTO;
using FoMart.Application.DTO.Sales;
using FoMart.Application.Interfaces.Repositories;
using FoMart.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoMart.Infrastructure.Repositories;

public sealed class SaleRepository(FoMartDbContext dbContext)
    : RepositoryBase<Sale>(dbContext), ISaleRepository
{
    // Tìm hóa đơn, mới nhất trước (theo seq, khóa cụm tăng dần).
    public async Task<PagedResult<SaleListItemResponse>> SearchAsync(
        SaleSearchRequest request, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default)
    {
        var query = DbContext.Sales.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(s => s.Code.Contains(keyword));
        }
        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(s => s.Status == request.Status);
        if (!string.IsNullOrWhiteSpace(request.PaymentMethod))
            query = query.Where(s => s.PaymentMethod == request.PaymentMethod);
        if (request.CreatedBy is { } createdBy)
            query = query.Where(s => s.CreatedBy == createdBy);
        if (fromUtc is { } from)
            query = query.Where(s => s.CreatedAt >= from);
        if (toUtc is { } to)
            query = query.Where(s => s.CreatedAt < to);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(s => s.Seq)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new SaleListItemResponse(
                s.Id, s.Code, s.Status, s.PaymentMethod, s.Total, s.SaleLines.Count,
                s.CreatedByNavigation.Name, s.CreatedAt, s.PaidAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<SaleListItemResponse>(items, request.Page, request.PageSize, total);
    }

    // Hóa đơn được theo dõi kèm dòng, vị trí, người bán, người hủy.
    public Task<Sale?> GetWithLinesAsync(Guid saleId, CancellationToken cancellationToken = default) =>
        DbContext.Sales
            .Include(s => s.SaleLines)
            .Include(s => s.Location)
            .Include(s => s.CreatedByNavigation)
            .Include(s => s.CancelledByNavigation)
            .AsSplitQuery()
            .SingleOrDefaultAsync(s => s.Id == saleId, cancellationToken);
}
