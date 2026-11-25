namespace FoMart.Domain.Enums;

// Loại vị trí, trùng CHECK CK_locations_type trong database.
public static class LocationType
{
    // Kho sau.
    public const string Storage = "storage";

    // Kệ bán: nơi bán hàng trừ kho (BR-16).
    public const string Shelf = "shelf";

    public static readonly IReadOnlyList<string> All = [Storage, Shelf];
}
