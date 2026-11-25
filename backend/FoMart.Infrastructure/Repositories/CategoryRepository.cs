using FoMart.Application.DTO.Catalog;
using FoMart.Application.Interfaces.Repositories;
using FoMart.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoMart.Infrastructure.Repositories;

public sealed class CategoryRepository(FoMartDbContext dbContext)
    : RepositoryBase<Category>(dbContext), ICategoryRepository
{
    // Danh sách nhóm hàng kèm số sản phẩm.
    public async Task<IReadOnlyList<CategoryResponse>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken = default) =>
        await DbContext.Categories.AsNoTracking()
            .Where(c => includeInactive || c.IsActive)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => new CategoryResponse(c.Id, c.Name, c.SortOrder, c.IsActive, c.Products.Count))
            .ToListAsync(cancellationToken);

    // Collation Latin1_General_100_CI_AI nên so sánh bỏ dấu, không phân biệt hoa thường (giống UQ_categories_name).
    public Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken cancellationToken = default) =>
        DbContext.Categories.AnyAsync(c => c.Name == name && c.Id != excludeId, cancellationToken);
}
