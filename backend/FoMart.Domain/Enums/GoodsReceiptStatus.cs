namespace FoMart.Domain.Enums;

// Trạng thái phiếu nhập, trùng CHECK CK_goods_receipts_status trong database.
public static class GoodsReceiptStatus
{
    // Nháp: còn sửa được, chưa đổi tồn.
    public const string Draft = "draft";

    // Đã hoàn tất: đã tạo lô và ghi sổ kho, không sửa được nữa (BR-03).
    public const string Completed = "completed";

    // Đã hủy bằng chứng từ đảo ngược (receipt_cancel), phiếu gốc giữ nguyên (BR-03).
    public const string Cancelled = "cancelled";

    public static readonly IReadOnlyList<string> All = [Draft, Completed, Cancelled];
}
