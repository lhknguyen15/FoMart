using FoMart.Application.DTO;
using FoMart.Application.DTO.Purchasing;
using FoMart.Domain.Entities;

namespace FoMart.Application.Interfaces.Repositories;

// Phiếu nhập cùng bảng con: receipt_lines.
public interface IGoodsReceiptRepository : IRepositoryBase<GoodsReceipt>
{
    // Tìm phiếu nhập có phân trang; createdFromUtc/createdToUtc là khoảng [from, to) theo UTC.
    Task<PagedResult<GoodsReceiptListItemResponse>> SearchAsync(
        GoodsReceiptSearchRequest request, DateTime? createdFromUtc, DateTime? createdToUtc, CancellationToken cancellationToken = default);

    // Lấy phiếu (được theo dõi để sửa) kèm các dòng, đơn vị, sản phẩm, nhà cung cấp, vị trí, người lập/hoàn tất/hủy.
    Task<GoodsReceipt?> GetWithLinesAsync(int receiptId, CancellationToken cancellationToken = default);

    // Xóa một dòng của phiếu nháp (phiếu đã hoàn tất không bao giờ xóa dòng, BR-03).
    void RemoveLine(ReceiptLine line);

    // Giá nhập lần gần nhất của một đơn vị, lấy từ phiếu đã hoàn tất (kể cả đã hủy sau đó thì bỏ qua).
    Task<LastPurchasePriceResponse?> GetLastPriceAsync(int unitId, CancellationToken cancellationToken = default);
}
