namespace FoMart.Application.DTO.Store;

public static class StoreResponseMessageDTO
{
    public const string GetStoreSuccess = "Lấy thông tin cửa hàng thành công.";
    public const string StoreNotConfigured = "Chưa có cấu hình cửa hàng. Hãy chạy database/migrate.ps1.";
    public const string GetPaymentSuccess = "Lấy tài khoản nhận chuyển khoản thành công.";
    public const string PaymentNotConfigured = "Cửa hàng chưa nhập tài khoản ngân hàng (mã BIN, số tài khoản, tên chủ tài khoản) nên chưa tạo được mã VietQR.";
}
