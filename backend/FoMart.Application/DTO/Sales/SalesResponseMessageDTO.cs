namespace FoMart.Application.DTO.Sales;

public static class SalesResponseMessageDTO
{
    // Hóa đơn
    public const string SearchSalesSuccess = "Lấy danh sách hóa đơn thành công.";
    public const string GetSaleSuccess = "Lấy hóa đơn thành công.";
    public const string CreatePaidSuccess = "Đã lưu hóa đơn và trừ kho.";
    public const string CreatePendingSuccess = "Đã lưu hóa đơn chờ chuyển khoản. Bấm \"Đã nhận\" khi khách chuyển xong.";
    public const string AlreadyRecorded = "Hóa đơn này đã được ghi trước đó (gửi lại cùng mã).";
    public const string ConfirmPaymentSuccess = "Đã nhận tiền chuyển khoản, hóa đơn đã thanh toán và trừ kho.";
    public const string CancelPendingSuccess = "Đã bỏ hóa đơn chờ thanh toán.";
    public const string CancelPaidSuccess = "Đã hủy hóa đơn và cộng tồn lại đúng các lô đã trừ.";
    public const string SaleNotFound = "Không tìm thấy hóa đơn.";
    public const string IdMustBeUuidV7 = "Mã hóa đơn phải là UUID v7 do máy bán sinh.";
    public const string IdUsedByOtherUser = "Mã hóa đơn này đã được dùng cho hóa đơn khác.";
    public const string InvalidSaleStatus = "Trạng thái hóa đơn không hợp lệ (pending_payment, paid, cancelled).";
    public const string InvalidPaymentMethod = "Hình thức thanh toán không hợp lệ (cash, transfer).";
    public const string InvalidDateRange = "Ngày bắt đầu phải trước hoặc bằng ngày kết thúc.";
    public const string CreatedAtInFuture = "Giờ bán không được ở tương lai.";
    public const string CashGivenOnlyForCash = "Chỉ ghi tiền khách đưa với hóa đơn tiền mặt.";
    public const string CashGivenTooLow = "Tiền khách đưa ít hơn tổng tiền.";
    public const string NotPending = "Hóa đơn không ở trạng thái chờ thanh toán.";
    public const string AlreadyCancelled = "Hóa đơn đã bị hủy trước đó.";
    public const string OnlyOwnerCancelsPaid = "Chỉ chủ tiệm được hủy hóa đơn đã thanh toán (UC-06).";
    public const string NoSaleLocation = "Cửa hàng chưa có vị trí bán mặc định (BR-16).";

    // Dòng hàng
    public const string UnitNotFound = "Không tìm thấy đơn vị sản phẩm (UnitId {0}).";
    public const string UnitInactive = "Đơn vị \"{0}\" đã ngừng dùng.";
    public const string ProductInactive = "Sản phẩm \"{0}\" đã ngừng kinh doanh.";
    public const string UndeclaredNeedsNameAndPrice = "Dòng {0}: hàng chưa khai báo cần tên và giá nhập tay.";
    public const string DeclaredHasNoManualPrice = "Dòng {0}: không sửa giá hàng đã khai báo, giá lấy theo đơn vị.";
    public const string ExpiredNeedsConfirm = "Chỉ còn lô hết hạn: {0}. Kiểm tra hàng trên tay rồi gửi lại với ConfirmExpired = true.";

    // Cảnh báo (không chặn bán)
    public const string WarnShort = "{0}: tồn tại kệ không đủ, đã trừ âm và đánh dấu cần kiểm kê (BR-01).";
    public const string WarnProvisional = "{0}: kệ chưa có lô nào, đã trừ vào lô tạm và đánh dấu cần kiểm kê (BR-01).";
    public const string WarnExpired = "{0}: đã bán từ lô hết hạn theo xác nhận của người bán (BR-05).";

    // Chung
    public const string Unauthorized = "Chưa đăng nhập hoặc phiên đăng nhập không hợp lệ.";
}
