namespace FoMart.Domain.Enums;

// Trạng thái phiếu xuất hủy (trùng CK_disposals_status). Chủ tiệm lập thì duyệt ngay (BR-15); nhân viên lập thì chờ duyệt (BR-21).
public static class DisposalStatus
{
    public const string Pending = "pending";
    public const string Approved = "approved";
    public const string Rejected = "rejected";

    public static readonly IReadOnlyList<string> All = [Pending, Approved, Rejected];
}
