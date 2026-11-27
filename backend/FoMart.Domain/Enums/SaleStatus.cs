namespace FoMart.Domain.Enums;

// Trạng thái hóa đơn, trùng CHECK CK_sales_status trong database.
public static class SaleStatus
{
    // Chuyển khoản chưa xác nhận: giữ giỏ, chưa trừ kho (UC-05).
    public const string PendingPayment = "pending_payment";

    // Đã thanh toán: đã trừ kho, tính doanh thu.
    public const string Paid = "paid";

    // Đã hủy (UC-06); nếu đã trừ kho thì đã cộng lại bằng sale_cancel.
    public const string Cancelled = "cancelled";

    public static readonly IReadOnlyList<string> All = [PendingPayment, Paid, Cancelled];
}
