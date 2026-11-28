namespace FoMart.Application.DTO.Store;

// GET /api/stores: thông tin cửa hàng hiển thị trên giao diện (UC-11).
public sealed record StoreResponse(
    string Name,
    string? Address,
    string? Phone,
    bool HasBankAccount);

// GET /api/stores/payment: tài khoản nhận chuyển khoản để vẽ mã VietQR (UC-05). Cần đăng nhập:
// endpoint công khai GET /api/stores không trả số tài khoản.
public sealed record StorePaymentResponse(
    string BankBin,
    string AccountNo,
    string AccountName);
