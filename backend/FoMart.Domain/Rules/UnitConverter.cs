using FoMart.Domain.Exceptions;

namespace FoMart.Domain.Rules;

// Quy đổi theo đơn vị cơ sở (BR-09, BR-11). Tồn kho luôn tính theo đơn vị cơ sở.
public static class UnitConverter
{
    // Số lượng theo đơn vị cơ sở = số lượng × hệ số; vượt giới hạn INT thì báo lỗi nghiệp vụ.
    public static int ToBaseQty(int qty, int factor)
    {
        if (qty <= 0) throw new BusinessRuleException("BR-09", "Số lượng phải lớn hơn 0.");
        if (factor < 1) throw new BusinessRuleException("BR-10", "Hệ số quy đổi phải là số nguyên dương.");

        var baseQty = (long)qty * factor;
        if (baseQty > int.MaxValue)
            throw new BusinessRuleException("BR-09", "Số lượng quy đổi ra đơn vị cơ sở quá lớn.");
        return (int)baseQty;
    }

    // BR-11: giá vốn một đơn vị cơ sở = giá nhập của đơn vị nhập ÷ hệ số, làm tròn 4 chữ số thập phân.
    public static decimal ToBaseUnitCost(long unitPrice, int factor)
    {
        if (unitPrice < 0) throw new BusinessRuleException("BR-11", "Giá nhập không được âm.");
        if (factor < 1) throw new BusinessRuleException("BR-10", "Hệ số quy đổi phải là số nguyên dương.");

        return Math.Round((decimal)unitPrice / factor, 4, MidpointRounding.AwayFromZero);
    }

    // Thành tiền một dòng (đồng) = đơn giá × số lượng; vượt giới hạn BIGINT thì báo lỗi.
    public static long LineTotal(long unitPrice, int qty)
    {
        try
        {
            return checked(unitPrice * qty);
        }
        catch (OverflowException)
        {
            throw new BusinessRuleException("BR-11", "Thành tiền quá lớn.");
        }
    }
}
