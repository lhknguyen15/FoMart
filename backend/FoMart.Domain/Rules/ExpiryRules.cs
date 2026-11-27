namespace FoMart.Domain.Rules;

// Mức cảnh báo hạn dùng khi nhập hàng (UC-04 ngoại lệ "hạn dùng đã qua hoặc còn dưới N ngày").
public enum ExpiryWarningLevel
{
    None,
    NearExpiry,
    Expired,
}

// Quy tắc hạn dùng thuần, không phụ thuộc đồng hồ hệ thống (truyền "hôm nay" vào để dễ test).
public static class ExpiryRules
{
    // Hạn dùng trước hôm nay: đã hết hạn; còn dưới warnDays ngày: sắp hết hạn.
    public static ExpiryWarningLevel Check(DateOnly? expiryDate, DateOnly today, int warnDays)
    {
        if (expiryDate is not { } expiry) return ExpiryWarningLevel.None;
        if (expiry < today) return ExpiryWarningLevel.Expired;
        return expiry.DayNumber - today.DayNumber < warnDays ? ExpiryWarningLevel.NearExpiry : ExpiryWarningLevel.None;
    }
}
