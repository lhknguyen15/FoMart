namespace FoMart.Domain.Enums;

// Hình thức thanh toán, trùng CHECK CK_sales_payment_method trong database.
public static class PaymentMethod
{
    // Tiền mặt: hóa đơn thanh toán ngay.
    public const string Cash = "cash";

    // Chuyển khoản VietQR: người bán tự xác nhận "Đã nhận" (không tích hợp ngân hàng).
    public const string Transfer = "transfer";

    public static readonly IReadOnlyList<string> All = [Cash, Transfer];
}
