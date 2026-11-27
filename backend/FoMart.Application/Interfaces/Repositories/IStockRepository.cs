using FoMart.Application.DTO;
using FoMart.Application.DTO.Inventory;
using FoMart.Domain.Entities;

namespace FoMart.Application.Interfaces.Repositories;

// Lõi kho: lots, stock_moves (chỉ thêm), đọc stock. Mọi nghiệp vụ đổi tồn đi qua đây (BR-02).
// KHÔNG có cách ghi bảng stock: thêm StockMove thì trigger TR_stock_moves_apply tự cộng vào stock.
// Ghi lô và sổ bằng AddLotAsync / AddAsync (kế thừa); Update/Delete trên StockMove bị trigger chặn (NFR-10).
public interface IStockRepository : IRepositoryBase<StockMove>
{
    // Thêm một lô mới (nhập hàng, kiểm kê dương, lô tạm).
    Task AddLotAsync(Lot lot, CancellationToken cancellationToken = default);

    // Khóa các dòng tồn của những lô này (UPDLOCK, ROWLOCK) theo thứ tự lot_id tăng dần, giữ đến hết transaction.
    // Phải gọi bên trong unitOfWork.BeginWriteAsync (NFR-06).
    Task LockLotsAsync(IReadOnlyCollection<int> lotIds, CancellationToken cancellationToken = default);

    // Các lô trong danh sách đã có biến động khác loại exceptDocType (bị bán, chuyển, hủy, kiểm kê...).
    Task<IReadOnlyList<int>> GetLotsMovedByOtherDocsAsync(
        IReadOnlyCollection<int> lotIds, string exceptDocType, CancellationToken cancellationToken = default);

    // Tồn theo lô và vị trí, có phân trang; lô hết hạn sớm đứng trước (FEFO).
    Task<PagedResult<StockLotResponse>> SearchStockAsync(StockSearchRequest request, CancellationToken cancellationToken = default);

    // Thẻ kho của một sản phẩm, mới nhất trước; khoảng [fromUtc, toUtc) theo UTC.
    Task<PagedResult<StockMoveResponse>> GetMovesAsync(
        StockMoveSearchRequest request, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default);

    // Các dòng tồn lệch khỏi sổ kho (view v_stock_ledger_mismatch); phải luôn rỗng.
    Task<IReadOnlyList<StockLedgerMismatchResponse>> GetLedgerMismatchesAsync(CancellationToken cancellationToken = default);
}
