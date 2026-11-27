using FoMart.Application.DTO.Catalog;
using FoMart.Domain.Entities;

namespace FoMart.Application.Interfaces.Repositories;

public interface ILocationRepository : IRepositoryBase<Location>
{
    // Danh sách vị trí kèm tổng tồn, vị trí bán mặc định đứng đầu.
    Task<IReadOnlyList<LocationResponse>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken = default);

    // Vị trí bán mặc định hiện tại (được theo dõi để sửa).
    Task<Location?> GetDefaultSaleAsync(CancellationToken cancellationToken = default);

    // Vị trí nhận hàng mặc định (UC-04): kho sau đang dùng nếu có, không thì vị trí bán mặc định.
    Task<Location?> GetDefaultReceivingAsync(CancellationToken cancellationToken = default);

    // Vị trí còn tồn khác 0 không.
    Task<bool> HasStockAsync(int locationId, CancellationToken cancellationToken = default);

    // Tổng tồn tại vị trí theo đơn vị cơ sở.
    Task<int> GetStockQtyAsync(int locationId, CancellationToken cancellationToken = default);

    // Tên vị trí đã tồn tại chưa (không phân biệt dấu, hoa thường).
    Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken cancellationToken = default);
}
