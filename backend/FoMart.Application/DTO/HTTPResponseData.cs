namespace FoMart.Application.DTO;

// Khuôn phản hồi chung của mọi API, kể cả khi lỗi.
// Service trả về, Controller trả nguyên cho client với HTTP status = StatusCode.
public sealed record HTTPResponseData<T>
{
    public required T DataResponse { get; init; }

    public required string Message { get; init; }

    public int StatusCode { get; init; }

    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

// Tạo nhanh HTTPResponseData trong Service.
public static class HTTPResponse
{
    // Thành công (200 mặc định, 201 khi tạo mới).
    public static HTTPResponseData<T?> Success<T>(T data, string message, int statusCode = 200) =>
        new() { DataResponse = data, Message = message, StatusCode = statusCode };

    // Thất bại: không có dữ liệu, chỉ có thông báo và mã lỗi (400/404/409...).
    public static HTTPResponseData<T?> Fail<T>(int statusCode, string message) =>
        new() { DataResponse = default, Message = message, StatusCode = statusCode };
}
