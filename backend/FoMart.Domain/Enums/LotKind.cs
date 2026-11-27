namespace FoMart.Domain.Enums;

// Loại lô, trùng CHECK CK_lots_kind trong database (ADR-0002).
public static class LotKind
{
    // Tạo từ một dòng phiếu nhập.
    public const string Receipt = "receipt";

    // Tạo từ kiểm kê dương hoặc tồn đầu kỳ.
    public const string CountAdjust = "count_adjust";

    // Lô tạm khi bán hàng chưa từng có lô tại kệ (BR-01); giá vốn NULL.
    public const string Provisional = "provisional";

    public static readonly IReadOnlyList<string> All = [Receipt, CountAdjust, Provisional];
}
