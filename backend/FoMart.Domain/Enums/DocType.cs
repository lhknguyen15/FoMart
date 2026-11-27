namespace FoMart.Domain.Enums;

// Loại chứng từ trên sổ kho, trùng CHECK CK_stock_moves_doc_type (docs/database.md mục 3).
public static class DocType
{
    // Hoàn tất phiếu nhập (+).
    public const string Receipt = "receipt";

    // Hủy phiếu nhập khi lô chưa bị trừ (−).
    public const string ReceiptCancel = "receipt_cancel";

    // Hóa đơn bán (−).
    public const string Sale = "sale";

    // Hủy hóa đơn, trả lại đúng các lô đã trừ (+).
    public const string SaleCancel = "sale_cancel";

    // Chuyển kho: ra khỏi vị trí nguồn (−) và vào vị trí đích (+).
    public const string TransferOut = "transfer_out";
    public const string TransferIn = "transfer_in";

    // Phiếu hủy được duyệt (−).
    public const string Disposal = "disposal";

    // Điều chỉnh kiểm kê, tồn đầu kỳ (±).
    public const string CountAdjust = "count_adjust";

    public static readonly IReadOnlyList<string> All =
        [Receipt, ReceiptCancel, Sale, SaleCancel, TransferOut, TransferIn, Disposal, CountAdjust];
}
