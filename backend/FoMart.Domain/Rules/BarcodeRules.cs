namespace FoMart.Domain.Rules;

// Chuẩn hóa mã vạch trước khi lưu hoặc tra (ADR-0003 mục 4).
public static class BarcodeRules
{
    // Bỏ khoảng trắng; rỗng → null. Mã UPC-A 12 chữ số thêm "0" đầu thành EAN-13,
    // vì cùng một vạch mã có bộ đọc trả 12 số, có bộ đọc trả 13 số.
    public static string? Normalize(string? barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return null;
        var trimmed = barcode.Trim();
        return trimmed.Length == 12 && trimmed.All(char.IsAsciiDigit) ? "0" + trimmed : trimmed;
    }
}
