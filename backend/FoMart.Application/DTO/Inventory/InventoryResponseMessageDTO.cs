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
}
