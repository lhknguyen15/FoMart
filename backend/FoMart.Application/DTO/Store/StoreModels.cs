namespace FoMart.Application.DTO.Store;

// GET /api/stores: thông tin cửa hàng hiển thị trên giao diện (UC-11).
public sealed record StoreResponse(
    string Name,
    string? Address,
    string? Phone,
    bool HasBankAccount);
