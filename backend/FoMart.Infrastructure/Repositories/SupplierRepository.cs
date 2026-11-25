using FoMart.Application.DTO.Catalog;
using FoMart.Application.Interfaces.Repositories;
using FoMart.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoMart.Infrastructure.Repositories;

public sealed class SupplierRepository(FoMartDbContext dbContext)
    : RepositoryBase<Supplier>(dbContext), ISupplierRepository
{
    // Tìm theo tên (collation bỏ dấu) hoặc số điện thoại.
    public async Task<IReadOnlyList<SupplierResponse>> SearchAsync(
        SupplierSearchRequest request, CancellationToken cancellationToken = default)
    {
        var query = DbContext.Suppliers.AsNoTracking();
        if (!request.IncludeInactive)
            query = query.Where(s => s.IsActive);
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(s => s.Name.Contains(keyword) || (s.Phone != null && s.Phone.Contains(keyword)));
        }

        return await query
            .OrderBy(s => s.Name)
            .Select(s => new SupplierResponse(s.Id, s.Name, s.Phone, s.Zalo, s.Note, s.IsActive, s.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
