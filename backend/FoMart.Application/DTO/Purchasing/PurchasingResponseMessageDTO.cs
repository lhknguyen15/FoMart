namespace FoMart.Application.DTO.Purchasing;

public static class PurchasingResponseMessageDTO
{
    // Phiếu nhập
    public const string SearchReceiptsSuccess = "Lấy danh sách phiếu nhập thành công.";
    public const string GetReceiptSuccess = "Lấy phiếu nhập thành công.";
    public const string CreateReceiptSuccess = "Tạo phiếu nhập nháp thành công.";
    public const string UpdateReceiptSuccess = "Cập nhật phiếu nhập thành công.";
    public const string DeleteReceiptSuccess = "Đã xóa phiếu nhập nháp.";
    public const string CompleteReceiptSuccess = "Hoàn tất phiếu nhập: đã tạo lô và cộng tồn.";
    public const string CancelReceiptSuccess = "Đã hủy phiếu nhập bằng chứng từ đảo ngược.";
    public const string ReceiptNotFound = "Không tìm thấy phiếu nhập.";
    public const string InvalidReceiptStatus = "Trạng thái phiếu nhập không hợp lệ (draft, completed, cancelled).";
    public const string InvalidDateRange = "Ngày bắt đầu phải trước hoặc bằng ngày kết thúc.";
    public const string ReceiptNotDraft = "Phiếu nhập đã hoàn tất hoặc đã hủy nên không sửa được (BR-03).";
    public const string ReceiptNotCompleted = "Chỉ hủy được phiếu nhập đã hoàn tất.";
    public const string ReceiptHasNoLines = "Phiếu nhập chưa có dòng nào.";
    public const string ReceiptMissingPrices = "Còn {0} dòng chưa có giá nhập. Chủ tiệm cần điền giá trước khi hoàn tất.";
    public const string ExpiryWarningNeedsConfirm = "Cảnh báo hạn dùng: {0}. Gửi lại với ConfirmExpiryWarning = true nếu vẫn nhập.";
    public const string ReceiptLotsAlreadyUsed = "Không hủy được: hàng của phiếu đã được bán, chuyển, hủy hoặc kiểm kê (lô {0}). Hãy dùng phiếu hủy hoặc kiểm kê.";

    // Dòng nhập
    public const string AddLineSuccess = "Thêm dòng nhập thành công.";
    public const string UpdateLineSuccess = "Cập nhật dòng nhập thành công.";
    public const string DeleteLineSuccess = "Đã xóa dòng nhập.";
    public const string LineNotFound = "Không tìm thấy dòng nhập trong phiếu này.";
    public const string UnitNotFound = "Không tìm thấy đơn vị sản phẩm (UnitId {0}).";
    public const string UnitInactive = "Đơn vị \"{0}\" đã ngừng dùng.";
    public const string ProductInactive = "Sản phẩm \"{0}\" đã ngừng kinh doanh.";
    public const string ExpiryRequired = "Sản phẩm \"{0}\" có quản lý hạn dùng: phải nhập hạn dùng.";
    public const string TooManyLines = "Phiếu nhập tối đa 500 dòng.";

    // Giá nhập
    public const string GetLastPriceSuccess = "Lấy giá nhập lần trước thành công.";
    public const string NoLastPrice = "Đơn vị này chưa từng được nhập.";
    public const string PriceHiddenForStaff = "Chủ tiệm đã ẩn giá nhập với nhân viên (BR-08): để trống giá, chủ tiệm sẽ điền.";

    // Tham chiếu
    public const string SupplierNotFound = "Không tìm thấy nhà cung cấp.";
    public const string SupplierInactive = "Nhà cung cấp đang bị ẩn.";
    public const string LocationNotFound = "Không tìm thấy vị trí nhận.";
    public const string LocationInactive = "Vị trí nhận đã ngừng dùng.";
    public const string NoReceivingLocation = "Cửa hàng chưa có vị trí nào đang dùng.";

    // Chung
    public const string Unauthorized = "Chưa đăng nhập hoặc phiên đăng nhập không hợp lệ.";
}
