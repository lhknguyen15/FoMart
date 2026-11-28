namespace FoMart.Domain.Rules;

// Một phần số lượng hủy lấy từ một lô; UnitCost là giá vốn lô (null: lô tạm) để tính tổn thất.
public sealed record DisposalAllocation(int LotId, int Qty, decimal? UnitCost);

// Kết quả chia lô khi hủy. Allocations rỗng khi không đủ hàng; Available = số còn tồn dương có thể hủy.
public sealed record DisposalAllocationResult(IReadOnlyList<DisposalAllocation> Allocations, int Available)
{
    public bool IsShort => Allocations.Count == 0;

    // Tổn thất theo giá vốn (UC-08 bước 3); null nếu có phần nằm ở lô tạm (giá vốn chưa biết).
    public decimal? LossTotal =>
        Allocations.Any(a => a.UnitCost is null) ? null : Allocations.Sum(a => a.Qty * a.UnitCost!.Value);
}

// Chọn lô để hủy tại một vị trí (UC-08). Khác bán hàng (BR-01): hủy là thao tác có kiểm tra như chuyển kho,
// nên KHÔNG cho tồn âm; thiếu thì từ chối để người dùng kiểm kê trước.
public static class DisposalAllocator
{
    // lotId có giá trị: chỉ hủy ở đúng lô đó (chọn từ màn hình hạn dùng).
    // lotId null: lấy từ các lô còn tồn dương, hạn dùng sớm nhất trước (lô đã hết hạn tự đứng đầu),
    // lô không có hạn dùng sau cùng, rồi theo ngày nhận (FIFO).
    public static DisposalAllocationResult Allocate(IReadOnlyCollection<LotStock> lots, int? lotId, int qty)
    {
        ArgumentNullException.ThrowIfNull(lots);
        if (qty <= 0) throw new ArgumentOutOfRangeException(nameof(qty), "Số lượng hủy phải lớn hơn 0.");

        var candidates = lots
            .Where(l => l.Qty > 0 && (lotId is null || l.LotId == lotId))
            .OrderBy(l => l.ExpiryDate is null).ThenBy(l => l.ExpiryDate).ThenBy(l => l.ReceivedAt).ThenBy(l => l.LotId)
            .ToList();
        var available = candidates.Sum(l => l.Qty);
        if (available < qty) return new DisposalAllocationResult([], available);

        var allocations = new List<DisposalAllocation>();
        var remaining = qty;
        foreach (var lot in candidates)
        {
            if (remaining == 0) break;
            var take = Math.Min(lot.Qty, remaining);
            allocations.Add(new DisposalAllocation(lot.LotId, take, lot.UnitCost));
            remaining -= take;
        }
        return new DisposalAllocationResult(allocations, available);
    }
}
