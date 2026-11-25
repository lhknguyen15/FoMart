namespace FoMart.Application.DTO;

// Khuôn dữ liệu phân trang dùng chung cho các API danh sách (nằm trong DataResponse).
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
