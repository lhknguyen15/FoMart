using FoMart.Domain.Enums;
using FoMart.Domain.Exceptions;

namespace FoMart.Domain.Rules;

// Tồn của một lô tại vị trí đang trừ (đơn vị cơ sở). Qty có thể âm (BR-01).
public sealed record LotStock(int LotId, string Kind, DateOnly? ExpiryDate, DateTime ReceivedAt, decimal? UnitCost, int Qty);

// Một phần số lượng trừ vào một lô. LotId = null: trừ vào lô tạm của sản phẩm (service tìm hoặc tạo).
public sealed record LotAllocation(int? LotId, int Qty, decimal? UnitCost, bool IsExpired);

// Kết quả chia lô.
// NeedsExpiredConfirmation: lô còn hạn không đủ, chỉ còn lô hết hạn mà người bán chưa xác nhận (BR-05); khi đó Allocations rỗng.
// IsShort: tồn không đủ, phần thiếu trừ âm (BR-01). UsesProvisional: có phần trừ vào lô tạm (giá vốn không biết).
public sealed record AllocationResult(
    IReadOnlyList<LotAllocation> Allocations,
    bool NeedsExpiredConfirmation,
    bool IsShort,
    bool UsesProvisional)
{
    // Tổng giá vốn (BR-06); null nếu có phần nằm ở lô tạm (giá vốn chưa biết).
    public decimal? CostTotal =>
        Allocations.Any(a => a.UnitCost is null) ? null : Allocations.Sum(a => a.Qty * a.UnitCost!.Value);

    public bool UsesExpired => Allocations.Any(a => a.IsExpired);
}

// Chia số lượng cần trừ vào các lô tại một vị trí: BR-01, BR-04, BR-05.
public static class FefoAllocator
{
    // Thứ tự trừ:
    // 1. Lô còn hạn có tồn dương: hạn dùng gần nhất trước, lô không có hạn dùng sau cùng, rồi ngày nhận (FIFO).
    // 2. Lô đã hết hạn có tồn dương, chỉ khi người bán đã xác nhận kiểm tra hàng trên tay (BR-05).
    // 3. Lô tạm còn tồn dương (ví dụ sau khi hủy hóa đơn).
    // 4. Còn thiếu: trừ âm vào lô mới nhất (theo ngày nhận) tại vị trí; vị trí chưa từng có lô thì trừ vào lô tạm (BR-01).
    public static AllocationResult Allocate(IReadOnlyCollection<LotStock> lots, int qty, DateOnly today, bool allowExpired)
    {
        ArgumentNullException.ThrowIfNull(lots);
        if (qty <= 0) throw new BusinessRuleException("BR-04", "Số lượng cần trừ phải lớn hơn 0.");

        var regular = lots.Where(l => l.Kind != LotKind.Provisional).ToList();
        var fresh = regular
            .Where(l => l.Qty > 0 && !IsExpired(l, today))
            .OrderBy(l => l.ExpiryDate is null).ThenBy(l => l.ExpiryDate).ThenBy(l => l.ReceivedAt).ThenBy(l => l.LotId);
        var expired = regular
            .Where(l => l.Qty > 0 && IsExpired(l, today))
            .OrderBy(l => l.ExpiryDate).ThenBy(l => l.ReceivedAt).ThenBy(l => l.LotId)
            .ToList();
        var provisionalWithStock = lots.Where(l => l.Kind == LotKind.Provisional && l.Qty > 0);

        var allocations = new List<LotAllocation>();
        var remaining = qty;

        remaining = Take(fresh, remaining, allocations, isExpired: false);

        if (remaining > 0 && expired.Count > 0)
        {
            if (!allowExpired) return new AllocationResult([], NeedsExpiredConfirmation: true, IsShort: false, UsesProvisional: false);
            remaining = Take(expired, remaining, allocations, isExpired: true);
        }

        remaining = Take(provisionalWithStock, remaining, allocations, isExpired: false);

        var isShort = remaining > 0;
        if (isShort)
        {
            var newest = regular.OrderByDescending(l => l.ReceivedAt).ThenByDescending(l => l.LotId).FirstOrDefault();
            if (newest is not null)
                AddTo(allocations, newest.LotId, remaining, newest.UnitCost, IsExpired(newest, today));
            else
            {
                var provisional = lots.FirstOrDefault(l => l.Kind == LotKind.Provisional);
                AddTo(allocations, provisional?.LotId, remaining, unitCost: null, isExpired: false);
            }
        }

        var usesProvisional = allocations.Any(a => a.UnitCost is null);
        return new AllocationResult(allocations, NeedsExpiredConfirmation: false, isShort, usesProvisional);
    }

    // Lô hết hạn khi hạn dùng trước hôm nay.
    public static bool IsExpired(LotStock lot, DateOnly today) =>
        lot is { ExpiryDate: { } expiry } && expiry < today;

    private static int Take(IEnumerable<LotStock> lots, int remaining, List<LotAllocation> allocations, bool isExpired)
    {
        foreach (var lot in lots)
        {
            if (remaining == 0) break;
            var take = Math.Min(lot.Qty, remaining);
            AddTo(allocations, lot.LotId, take, lot.UnitCost, isExpired);
            remaining -= take;
        }
        return remaining;
    }

    // Gộp vào phần đã có của cùng lô (khi lô mới nhất vừa bị trừ phần dương rồi lại nhận phần âm).
    private static void AddTo(List<LotAllocation> allocations, int? lotId, int qty, decimal? unitCost, bool isExpired)
    {
        var index = allocations.FindIndex(a => a.LotId == lotId);
        if (index >= 0)
            allocations[index] = allocations[index] with { Qty = allocations[index].Qty + qty };
        else
            allocations.Add(new LotAllocation(lotId, qty, unitCost, isExpired));
    }
}
