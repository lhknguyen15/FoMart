using FoMart.Application.DTO;
using FoMart.Application.DTO.Inventory;
using FoMart.Domain.Entities;

namespace FoMart.Application.Interfaces.Repositories;

// Phiếu xuất hủy cùng bảng con: disposal_lines.
public interface IDisposalRepository : IRepositoryBase<Disposal>
{
    // Tìm phiếu hủy có phân trang, mới nhất trước; phiếu chờ duyệt lọc bằng Status = pending.
    Task<PagedResult<DisposalListItemResponse>> SearchAsync(DisposalSearchRequest request, CancellationToken cancellationToken = default);

    // Lấy phiếu (được theo dõi để duyệt / từ chối) kèm dòng, sản phẩm (đơn vị), vị trí, lô, người lập / duyệt.
    Task<Disposal?> GetWithLinesAsync(int disposalId, CancellationToken cancellationToken = default);

    // Các dòng sổ kho của một phiếu hủy (kèm lô), để hiện lô đã trừ và tổn thất.
    Task<IReadOnlyList<StockMove>> GetMovesAsync(int disposalId, CancellationToken cancellationToken = default);
}
