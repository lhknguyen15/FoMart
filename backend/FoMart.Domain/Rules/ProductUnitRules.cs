using FoMart.Domain.Exceptions;

namespace FoMart.Domain.Rules;

// Thông tin một đơn vị để kiểm tra quy tắc (đã chuẩn hóa: tên và mã vạch đã trim, mã vạch rỗng = null).
public sealed record UnitSpec(string Name, int Factor, string? Barcode, long SalePrice, bool IsBase);

// Cảnh báo giá đơn vị lớn cao hơn hệ số × giá đơn vị cơ sở (UC-01: cảnh báo, vẫn lưu nếu xác nhận).
public sealed record UnitPriceWarning(string UnitName, int Factor, long SalePrice, long BaseSalePrice)
{
    public long MaxReasonablePrice => Factor * BaseSalePrice;
}

// Quy tắc sản phẩm và đơn vị: BR-09, BR-10, BR-12, BR-20.
public static class ProductUnitRules
{
    // Kiểm tra bộ đơn vị của một sản phẩm; vi phạm thì ném BusinessRuleException.
    public static void EnsureValidUnitSet(IReadOnlyCollection<UnitSpec> units)
    {
        ArgumentNullException.ThrowIfNull(units);

        if (units.Count == 0)
            throw new BusinessRuleException("BR-09", "Sản phẩm phải có ít nhất một đơn vị.");

        var baseUnits = units.Where(u => u.IsBase).ToList();
        if (baseUnits.Count != 1)
            throw new BusinessRuleException("BR-09", "Sản phẩm phải có đúng một đơn vị cơ sở.");
        if (baseUnits[0].Factor != 1)
            throw new BusinessRuleException("BR-09", "Đơn vị cơ sở phải có hệ số quy đổi bằng 1.");

        foreach (var unit in units)
        {
            if (string.IsNullOrWhiteSpace(unit.Name))
                throw new BusinessRuleException("BR-10", "Tên đơn vị không được để trống.");
            if (unit.Factor < 1)
                throw new BusinessRuleException("BR-10", $"Hệ số quy đổi của \"{unit.Name}\" phải là số nguyên dương.");
            if (unit.SalePrice < 0)
                throw new BusinessRuleException("BR-10", $"Giá bán của \"{unit.Name}\" không được âm.");
        }

        var duplicateName = units.GroupBy(u => u.Name, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
        if (duplicateName is not null)
            throw new BusinessRuleException("BR-10", $"Tên đơn vị \"{duplicateName.Key}\" bị trùng.");

        var duplicateBarcode = units.Where(u => u.Barcode is not null)
            .GroupBy(u => u.Barcode!, StringComparer.Ordinal).FirstOrDefault(g => g.Count() > 1);
        if (duplicateBarcode is not null)
            throw new BusinessRuleException("BR-20", $"Mã vạch {duplicateBarcode.Key} bị dùng cho nhiều đơn vị.");
    }

    // Tìm các đơn vị có giá cao hơn hệ số × giá đơn vị cơ sở (chỉ cảnh báo, không chặn).
    public static IReadOnlyList<UnitPriceWarning> FindPriceWarnings(long baseSalePrice, IEnumerable<UnitSpec> units) =>
        units.Where(u => !u.IsBase && u.SalePrice > (long)u.Factor * baseSalePrice)
             .Select(u => new UnitPriceWarning(u.Name, u.Factor, u.SalePrice, baseSalePrice))
             .ToList();

    // BR-12: không đổi hệ số quy đổi khi đơn vị đã có giao dịch.
    public static void EnsureFactorChangeAllowed(string unitName, int currentFactor, int newFactor, bool hasTransactions)
    {
        if (currentFactor != newFactor && hasTransactions)
            throw new BusinessRuleException("BR-12",
                $"Đơn vị \"{unitName}\" đã có giao dịch nên không sửa được hệ số quy đổi. Hãy tạo đơn vị mới.");
    }
}
