using FoMart.Application.DTO.Catalog;
using FoMart.Domain.Entities;

namespace FoMart.Application.Interfaces.Repositories;

public interface ICategoryRepository : IRepositoryBase<Category>
{
    // Danh sách nhóm hàng kèm số sản phẩm, sắp theo SortOrder rồi tên.
    Task<IReadOnlyList<CategoryResponse>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken = default);

    // Tên nhóm hàng đã tồn tại chưa (không phân biệt dấu, hoa thường).
    Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken cancellationToken = default);
}
