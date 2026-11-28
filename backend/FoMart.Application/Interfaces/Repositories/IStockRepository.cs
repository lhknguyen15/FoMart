using FoMart.Application.DTO;
using FoMart.Application.DTO.Inventory;
using FoMart.Domain.Entities;
using FoMart.Domain.Rules;

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

    // Khóa dòng sản phẩm (UPDLOCK, ROWLOCK) theo product_id tăng dần, giữ đến hết transaction.
    // Hai lượt bán cùng sản phẩm phải chờ nhau, kể cả khi vị trí chưa có dòng tồn nào để khóa (NFR-06).
    Task LockProductsAsync(IReadOnlyCollection<int> productIds, CancellationToken cancellationToken = default);

    // Tồn theo lô của các sản phẩm tại một vị trí (khóa UPDLOCK, ROWLOCK), kèm thông tin lô để chia FEFO.
    Task<IReadOnlyDictionary<int, IReadOnlyList<LotStock>>> GetLotStocksAsync(
        IReadOnlyCollection<int> productIds, int locationId, CancellationToken cancellationToken = default);

    // Lô tạm của sản phẩm (mỗi sản phẩm tối đa một, UX_lots_provisional); null nếu chưa có.
    Task<int?> GetProvisionalLotIdAsync(int productId, CancellationToken cancellationToken = default);

    // Đánh dấu sản phẩm "cần kiểm kê" (BR-01, BR-14). Chạy ngay trong transaction hiện tại.
    Task MarkNeedsCountAsync(IReadOnlyCollection<int> productIds, CancellationToken cancellationToken = default);

    // Các dòng sổ kho của một hóa đơn (kèm lô), theo id tăng dần.
    Task<IReadOnlyList<StockMove>> GetSaleMovesAsync(Guid saleId, CancellationToken cancellationToken = default);

    // Các lô trong danh sách đã có biến động khác loại exceptDocType (bị bán, chuyển, hủy, kiểm kê...).
    Task<IReadOnlyList<int>> GetLotsMovedByOtherDocsAsync(
        IReadOnlyCollection<int> lotIds, string exceptDocType, CancellationToken cancellationToken = default);

    // Tồn theo lô và vị trí, có phân trang; lô hết hạn sớm đứng trước (FEFO).
    // expiryBefore có giá trị: chỉ lô còn tồn có hạn dùng trong [expiryFrom, expiryBefore), hạn sớm nhất đứng đầu (UC-08).
    Task<PagedResult<StockLotResponse>> SearchStockAsync(
        StockSearchRequest request, DateOnly? expiryFrom, DateOnly? expiryBefore, CancellationToken cancellationToken = default);

    // Tổng hợp lô còn tồn đã hết hạn (trước today) và sắp hết hạn ([today, nearBefore)), kèm giá trị theo giá vốn.
    Task<StockExpirySummaryResponse> GetExpirySummaryAsync(
        DateOnly today, DateOnly nearBefore, int warnDays, CancellationToken cancellationToken = default);

    // Thẻ kho của một sản phẩm, mới nhất trước; khoảng [fromUtc, toUtc) theo UTC.
    Task<PagedResult<StockMoveResponse>> GetMovesAsync(
        StockMoveSearchRequest request, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default);

    // Các dòng tồn lệch khỏi sổ kho (view v_stock_ledger_mismatch); phải luôn rỗng.
    Task<IReadOnlyList<StockLedgerMismatchResponse>> GetLedgerMismatchesAsync(CancellationToken cancellationToken = default);
}
