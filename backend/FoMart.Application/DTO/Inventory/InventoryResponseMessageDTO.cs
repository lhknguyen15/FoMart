namespace FoMart.Application.DTO.Inventory;

public static class InventoryResponseMessageDTO
{
    // Tồn kho
    public const string SearchStockSuccess = "Lấy tồn kho theo lô thành công.";
    public const string GetMovesSuccess = "Lấy thẻ kho thành công.";
    public const string LedgerConsistent = "Sổ kho khớp với tồn kho.";
    public const string LedgerMismatch = "Phát hiện {0} dòng tồn lệch khỏi sổ kho.";
    public const string ProductNotFound = "Không tìm thấy sản phẩm.";
    public const string InvalidDateRange = "Ngày bắt đầu phải trước hoặc bằng ngày kết thúc.";
    public const string InvalidExpiryFilter = "Bộ lọc hạn dùng không hợp lệ (warning, expired, near_expiry).";
    public const string GetExpirySummarySuccess = "Lấy tổng hợp hạn dùng thành công.";
    public const string SearchDisposalsSuccess = "Lấy danh sách phiếu hủy thành công.";
    public const string GetDisposalSuccess = "Lấy phiếu hủy thành công.";
    public const string CreateDisposalApproved = "Đã lập phiếu hủy và trừ tồn.";
    public const string CreateDisposalPending = "Đã lập phiếu hủy, chờ chủ tiệm duyệt (BR-21).";
    public const string ApproveDisposalSuccess = "Đã duyệt phiếu hủy và trừ tồn.";
    public const string RejectDisposalSuccess = "Đã từ chối phiếu hủy, tồn giữ nguyên.";
    public const string DisposalNotFound = "Không tìm thấy phiếu hủy.";
    public const string DisposalNotPending = "Phiếu hủy đã được duyệt hoặc từ chối trước đó.";
    public const string InvalidDisposalStatus = "Trạng thái phiếu hủy không hợp lệ (pending, approved, rejected).";
    public const string InvalidDisposalReason = "Dòng {0}: lý do hủy không hợp lệ (expired, broken, damaged, other).";
    public const string DisposalProductNotFound = "Dòng {0}: không tìm thấy sản phẩm.";
    public const string DisposalLocationNotFound = "Dòng {0}: không tìm thấy vị trí.";
    public const string DisposalLotNotFound = "Dòng {0}: lô không thuộc sản phẩm này.";
    public const string DisposalShortStock = "Dòng {0}: {1} tại {2} chỉ còn {3} {4} trong hệ thống, không hủy được {5}. Hãy kiểm kê trước hoặc giảm số lượng.";
    public const string Unauthorized = "Chưa đăng nhập hoặc phiên đăng nhập không hợp lệ.";
}
