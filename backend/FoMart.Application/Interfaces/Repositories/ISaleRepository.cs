using FoMart.Application.DTO;
using FoMart.Application.DTO.Sales;
using FoMart.Domain.Entities;

namespace FoMart.Application.Interfaces.Repositories;

// Hóa đơn cùng bảng con: sale_lines.
public interface ISaleRepository : IRepositoryBase<Sale>
{
    // Tìm hóa đơn có phân trang, mới nhất trước; khoảng [fromUtc, toUtc) theo giờ bán (UTC).
    Task<PagedResult<SaleListItemResponse>> SearchAsync(
        SaleSearchRequest request, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default);

    // Lấy hóa đơn (được theo dõi để sửa) kèm các dòng, vị trí, người bán, người hủy.
    Task<Sale?> GetWithLinesAsync(Guid saleId, CancellationToken cancellationToken = default);
}
