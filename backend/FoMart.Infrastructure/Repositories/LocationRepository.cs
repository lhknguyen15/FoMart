using FoMart.Application.DTO.Catalog;
using FoMart.Application.Interfaces.Repositories;
using FoMart.Domain.Entities;
using FoMart.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FoMart.Infrastructure.Repositories;

public sealed class LocationRepository(FoMartDbContext dbContext)
    : RepositoryBase<Location>(dbContext), ILocationRepository
{
    // Danh sách vị trí kèm tổng tồn; vị trí bán mặc định đứng đầu.
    public async Task<IReadOnlyList<LocationResponse>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken = default) =>
        await DbContext.Locations.AsNoTracking()
            .Where(l => includeInactive || l.IsActive)
            .OrderByDescending(l => l.IsDefaultSale).ThenBy(l => l.Name)
            .Select(l => new LocationResponse(
                l.Id, l.Name, l.Type, l.IsDefaultSale, l.IsActive,
                l.Stocks.Sum(s => (int?)s.Qty) ?? 0))
            .ToListAsync(cancellationToken);

    // Vị trí bán mặc định hiện tại.
    public Task<Location?> GetDefaultSaleAsync(CancellationToken cancellationToken = default) =>
        DbContext.Locations.SingleOrDefaultAsync(l => l.IsDefaultSale, cancellationToken);

    // UC-04: nhận hàng vào kho sau đang dùng (nhiều kho thì lấy kho tạo trước), không có thì vào vị trí bán mặc định.
    public async Task<Location?> GetDefaultReceivingAsync(CancellationToken cancellationToken = default) =>
        await DbContext.Locations
            .Where(l => l.IsActive && l.Type == LocationType.Storage)
            .OrderBy(l => l.Id)
            .FirstOrDefaultAsync(cancellationToken)
        ?? await GetDefaultSaleAsync(cancellationToken);

    // Còn dòng tồn khác 0 (kể cả âm, BR-01) thì coi là còn hàng.
    public Task<bool> HasStockAsync(int locationId, CancellationToken cancellationToken = default) =>
        DbContext.Stocks.AnyAsync(s => s.LocationId == locationId && s.Qty != 0, cancellationToken);

    // Tổng tồn tại vị trí.
    public async Task<int> GetStockQtyAsync(int locationId, CancellationToken cancellationToken = default) =>
        await DbContext.Stocks.Where(s => s.LocationId == locationId).SumAsync(s => (int?)s.Qty, cancellationToken) ?? 0;

    // So sánh bỏ dấu, không phân biệt hoa thường (giống UQ_locations_name).
    public Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken cancellationToken = default) =>
        DbContext.Locations.AnyAsync(l => l.Name == name && l.Id != excludeId, cancellationToken);
}
