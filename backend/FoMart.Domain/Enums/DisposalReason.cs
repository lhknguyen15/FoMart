namespace FoMart.Domain.Enums;

// Lý do hủy một dòng (trùng CK_disposal_lines_reason, UC-08 bước 2).
public static class DisposalReason
{
    public const string Expired = "expired";
    public const string Broken = "broken";
    public const string Damaged = "damaged";
    public const string Other = "other";

    public static readonly IReadOnlyList<string> All = [Expired, Broken, Damaged, Other];
}
