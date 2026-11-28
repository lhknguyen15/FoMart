namespace FoMart.Domain.Rules;

// Tiền trên hóa đơn, doanh thu, lãi là số nguyên đồng; giá vốn lưu DECIMAL(14,4) theo đơn vị cơ sở (BR-11).
// Khi đưa giá vốn ra con số tiền (tổng giá vốn, lãi gộp) thì làm tròn về đồng ở MỘT chỗ này.
public static class MoneyRules
{
    // Làm tròn một số tiền lẻ về đồng, nửa đồng làm tròn ra xa số 0 (7.916,5 → 7.917; -0,5 → -1).
    public static long ToDong(decimal amount) =>
        (long)Math.Round(amount, 0, MidpointRounding.AwayFromZero);

    // Lãi gộp = doanh thu − giá vốn đã làm tròn, để giá vốn + lãi luôn cộng đúng bằng doanh thu.
    public static long GrossProfit(long revenue, decimal exactCost) => revenue - ToDong(exactCost);
}
